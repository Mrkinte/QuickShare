using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using QuickShare.Models;
using QuickShare.Services;
using Serilog;
using System.IO;
using System.Security.Claims;

namespace QuickShare.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransmitController(
        ILogger logger,
        SqliteService sqliteService,
        AppConfigService appConfigService,
        RequestConfirmService requestConfirmService) : ControllerBase
    {
        private readonly List<FileProps> _files = new();
        private readonly object _filesLock = new();

        /// <summary>
        /// 请求登录。
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
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

                logger.Information($"{GetRemoteIp()} Login successfully.");
                return Ok(new { message = "Successful" });
            }
            else
            {
                logger.Warning($"{GetRemoteIp()} Login failure, Password is: {password}");
                return Unauthorized();  // 401
            }
        }

        /// <summary>
        /// 返回一个HTTP响应，该响应表明当前用户是否已通过身份验证。
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("logged")]
        public async Task<ActionResult> LoggedIn()
        {
            return Ok(new { isAuthenticated = User.Identity?.IsAuthenticated ?? false });
        }

        /// <summary>
        /// 是否启用上传文件自动分类功能。
        /// </summary>
        /// <param name="autoSorting">true-启用 false-禁用</param>
        /// <returns></returns>
        [HttpPost("sorting")]
        public IActionResult SetAutoSorting([FromForm] bool autoSorting)
        {
            appConfigService.TransmitConfig.AutoSorting = autoSorting;
            appConfigService.SaveConfig();
            return Ok(new { message = "Successful" });
        }

        /// <summary>
        /// 获取指定目录下的文件列表。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost("files")]
        public ActionResult<IEnumerable<FileRecordModel>> GetFiles([FromForm] string path)
        {

            if (path.Contains("..") || path.Contains(":"))
            {
                return BadRequest(new { error = "Invalid file path." });
            }
            string fullPath = (appConfigService.TransmitConfig.SavePath + path).Replace("/", "\\");
            if (!Directory.Exists(fullPath))
            {
                return NotFound();
            }

            lock (_filesLock)
            {
                _files.Clear();
                var directorys = Directory.GetDirectories(fullPath);
                foreach (var dirPath in directorys)
                {
                    var dirItem = new FileProps
                    {
                        Name = Path.GetFileName(dirPath),
                        Extension = ".folder",
                    };
                    _files.Add(dirItem);
                }

                var files = Directory.GetFiles(fullPath);
                foreach (var filePath in files)
                {
                    var fileItem = new FileProps
                    {
                        Name = Path.GetFileName(filePath),
                        Extension = Path.GetExtension(filePath),
                    };
                    _files.Add(fileItem);
                }
            }
            return Ok(_files);
        }

        /// <summary>
        /// 处理上传的文件，并将它们保存到硬盘中。
        /// </summary>
        /// <param name="path">文件保存路径</param>
        /// <returns></returns>
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<ActionResult> UploadFile([FromForm] string path)
        {
            try
            {
                // ensure upload directory exists.
                if (!Directory.Exists(appConfigService.TransmitConfig.SavePath))
                {
                    Directory.CreateDirectory(appConfigService.TransmitConfig.SavePath);
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
                        logger.Error("File upload failed: Total size exceeds the limit.");
                        return StatusCode(413, new { error = "File upload failed: Total size exceeds the limit." });
                    }

                    var fileName = file.FileName;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        continue;
                    }

                    if (path.Contains("..") || path.Contains(":"))
                    {
                        return BadRequest(new { error = "Invalid file path." });
                    }
                    string filePath = string.Empty;
                    if (appConfigService.TransmitConfig.AutoSorting)
                    {
                        var sortingRules = sqliteService.ReadAllSortingRules();
                        string extension = Path.GetExtension(fileName).ToLower();
                        var rule = sortingRules.Find(rule => rule.Extension.Any(x => x == extension));
                        if (rule != null)
                        {
                            string folderPath = appConfigService.TransmitConfig.SavePath + rule.SavePath;
                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }
                            filePath = (folderPath + "\\" + fileName).Replace("/", "\\");
                        }
                        else
                        {
                            filePath = (appConfigService.TransmitConfig.SavePath + path + fileName).Replace("/", "\\");
                        }
                    }
                    else
                    {
                        filePath = (appConfigService.TransmitConfig.SavePath + path + fileName).Replace("/", "\\");
                    }
                    // Ensure unique file name
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var newFileName = $"{nameWithoutExt}_{counter}{ext}";
                        filePath = (appConfigService.TransmitConfig.SavePath + path + newFileName).Replace("/", "\\");
                        counter++;
                    }

                    // Stream writing to files
                    using (var sourceStream = file.OpenReadStream())
                    using (var targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await sourceStream.CopyToAsync(targetStream, 8192, HttpContext.RequestAborted);
                        logger.Information($"File uploaded successfully: {filePath}");
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
                logger.Error($"File upload failed: {ex.Message}");
                return StatusCode(500, new { error = $"File upload failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// 新建文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost("create-folder")]
        public ActionResult CreateFolder([FromForm] string path)
        {
            try
            {
                if (path.Contains("..") || path.Contains(":"))
                {
                    return BadRequest(new { error = "Invalid file path." });
                }
                var folderPath = (appConfigService.TransmitConfig.SavePath + path).Replace("/", "\\");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    return Ok(new { message = "Successful" });
                }
                else
                {
                    return BadRequest(new { error = "Existed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to create folder: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取文件详细信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost("file-info")]
        public ActionResult GetFileInfo([FromForm] string path)
        {
            try
            {
                if (path.Contains("..") || path.Contains(":"))
                {
                    return BadRequest(new { error = "Invalid file path." });
                }
                var filePath = (appConfigService.TransmitConfig.SavePath + path).Replace("/", "\\");
                if (System.IO.File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    var result = new
                    {
                        name = fileInfo.Name,
                        size = fileInfo.Length,
                        extension = fileInfo.Extension,
                        creationTime = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        lastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    return Ok(result);
                }
                else if (Directory.Exists(filePath))
                {
                    var directoryInfo = new DirectoryInfo(filePath);
                    var result = new
                    {
                        name = directoryInfo.Name,
                        size = 0,
                        extension = ".folder",
                        creationTime = directoryInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        lastModified = directoryInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    return Ok(result);
                }
                return NotFound(new { error = "File does not exist." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to get file info: {ex.Message}" });
            }
        }

        /// <summary>
        /// 下载指定文件。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("download/{*path}")]
        public IActionResult DownloadFile(string path)
        {
            try
            {
                if (path.Contains("..") || path.Contains(":"))
                {
                    return BadRequest(new { error = "Invalid file path." });
                }
                var filePath = (appConfigService.TransmitConfig.SavePath + "\\" + path).Replace("/", "\\");

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

        [HttpPost("send-message")]
        public ActionResult SendMessage([FromForm] string message)
        {
            requestConfirmService.CreateMessageRequest("管理员", message);
            return Ok();
        }

        #region Private Methods

        /// <summary>
        /// 获取访问者的IP地址。
        /// </summary>
        /// <returns></returns>
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
}
