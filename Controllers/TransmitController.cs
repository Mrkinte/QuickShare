using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using QuickShare.Services;
using System.IO;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace QuickShare.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TransmitController(
        AppConfigService appConfigService,
        OnlineCountService onlineCountService,
        ILogger<TransmitController> logger) : ControllerBase
    {
        private readonly List<FileItem> _fileItems = new();
        private readonly object _fileItemsLock = new();
        private readonly string _saveDirectory = appConfigService.TransmitConfig.SavePath;

        [AllowAnonymous]
        [HttpGet("alive/{uuid}")]
        public async Task<ActionResult> Alive(string uuid)
        {
            var actualIp = GetRemoteIp();
            if (string.IsNullOrWhiteSpace(uuid))
                return BadRequest("Uuid is required");
            onlineCountService.UpdateVisitorActivity(string.IsNullOrEmpty(actualIp) ? uuid : actualIp);
            return Ok(new { status = "alive" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromForm] string password)
        {
            var storedPassword = appConfigService.TransmitConfig.Password;
            if ((password == storedPassword) && !(string.IsNullOrEmpty(storedPassword)))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "transmitUser"),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                };
                var identity = new ClaimsIdentity(claims, "Cookie");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("Cookie", principal);

                logger.LogInformation($"{GetRemoteIp()} Login successfully.");
                return Ok(new { message = "Login successfully." });
            }
            else
            {
                logger.LogWarning($"{GetRemoteIp()} Login failure, Password is: {password}");
                return Unauthorized(new { error = "Login failure." });
            }
        }

        /// <summary>
        /// Returns an HTTP response indicating whether the current user is authenticated.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("logged")]
        public async Task<ActionResult> LoggedIn()
        {
            return Ok(new { isAuthenticated = User.Identity?.IsAuthenticated ?? false });
        }

        [HttpGet("parameter")]
        public IActionResult GetParameters()
        {
            return Ok(new { maxFileSize = appConfigService.TransmitConfig.MaxFileSize });
        }

        /// <summary>
        /// Get root directory file list.
        /// </summary>
        /// <returns></returns>
        [HttpGet("files")]
        public ActionResult<IEnumerable<FileItem>> GetRootFiles()
        {
            SyncFileList();
            return Ok(_fileItems);
        }

        /// <summary>
        /// Get specified directory file list.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("files/{*path}")]
        public ActionResult<IEnumerable<FileItem>> GetFiles(string path)
        {
            SyncFileList(path);
            return Ok(_fileItems);
        }

        /// <summary>
        /// Process the uploaded files and save them to the hard drive.
        /// </summary>
        /// <returns></returns>
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<ActionResult> UploadFile()
        {
            try
            {
                // ensure upload directory exists.
                if (!Directory.Exists(_saveDirectory))
                {
                    Directory.CreateDirectory(_saveDirectory);
                }

                if (!Request.HasFormContentType)
                {
                    return BadRequest(new { error = "Request content type must be multipart/form-data." });
                }

                var form = await Request.ReadFormAsync(HttpContext.RequestAborted);
                var files = form.Files;

                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { error = "Uploaded file is empty." });
                }

                long totalSize = 0;
                var uploadedFiles = new List<string>();

                foreach (var file in files)
                {
                    totalSize += file.Length;
                    if (totalSize > appConfigService.TransmitConfig.MaxFileSize * 1024L * 1024)
                    {
                        logger.LogError("File upload failed: Total size exceeds the limit.");
                        return StatusCode(413, new { error = "File upload failed: Total size exceeds the limit." });
                    }

                    var fileName = file.FileName;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        continue;
                    }

                    // Ensure unique file name
                    var filePath = Path.Combine(_saveDirectory, fileName);
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var newFileName = $"{nameWithoutExt}_{counter}{ext}";
                        filePath = Path.Combine(_saveDirectory, newFileName);
                        counter++;
                    }

                    // Stream writing to files
                    using (var sourceStream = file.OpenReadStream())
                    using (var targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await sourceStream.CopyToAsync(targetStream, 8192, HttpContext.RequestAborted);
                        logger.LogInformation($"File uploaded successfully: {filePath}");
                    }

                    uploadedFiles.Add(Path.GetFileName(filePath));
                }

                var result = new
                {
                    successFiles = uploadedFiles,
                    successCount = uploadedFiles.Count,
                };
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, new { error = "Upload cancelled." });
            }
            catch (Exception ex)
            {
                logger.LogError($"File upload failed: {ex.Message}");
                return StatusCode(500, new { error = $"File upload failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Returns the specified file as a downloadable response to the client.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("download/{*path}")]
        public IActionResult DownloadFile(string path)
        {
            try
            {
                var filePath = Path.Combine(_saveDirectory, path);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "File does not exist." });
                }

                var fileProvider = new FileExtensionContentTypeProvider();
                if (!fileProvider.TryGetContentType(path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
                return File(fileStream, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"File download failed: {ex.Message}" });
            }
        }

        #region Private Methods
        /// <summary>
        /// Update file list
        /// </summary>
        /// <param name="relativePath"></param>
        private void SyncFileList(string relativePath = "")
        {
            string fullPath = Path.GetFullPath(Path.Combine(_saveDirectory, relativePath));
            if (!Directory.Exists(fullPath)) return;

            lock (_fileItemsLock)
            {
                _fileItems.Clear();
                var directorys = Directory.GetDirectories(fullPath);
                foreach (var dirPath in directorys)
                {
                    var dirName = Path.GetFileName(dirPath);
                    var dirInfo = new DirectoryInfo(dirPath);

                    var dirItem = new FileItem
                    {
                        Name = dirName,
                        Size = 0,
                        Type = "folder",
                        Url = Path.GetRelativePath(_saveDirectory, dirPath).Replace("\\", "/"),
                        CreateTime = dirInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    _fileItems.Add(dirItem);
                }

                var files = Directory.GetFiles(fullPath);
                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileInfo = new FileInfo(filePath);

                    var fileItem = new FileItem
                    {
                        Name = fileName,
                        Size = fileInfo.Length,
                        Type = "file",
                        Url = Path.GetRelativePath(_saveDirectory, filePath).Replace("\\", "/"),
                        CreateTime = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    _fileItems.Add(fileItem);
                }
            }
        }

        private string GetRemoteIp()
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            string actualIp = "";
            if (remoteIp != null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    actualIp = remoteIp.MapToIPv4().ToString();
                }
                else
                {
                    actualIp = remoteIp.ToString();
                }
            }
            return actualIp;
        }

        #endregion
    }

    public class FileItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("createTime")]
        public string CreateTime { get; set; } = string.Empty;
    }
}
