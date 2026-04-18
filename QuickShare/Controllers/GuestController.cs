using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickShare.Services;
using Serilog;
using System.IO;
using Path = System.IO.Path;

namespace QuickShare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuestController(
        ILogger logger,
        AppConfigService appConfigService,
        RequestConfirmService requestConfirmService) : ControllerBase
    {
        [HttpPost("request-upload")]
        public ActionResult RequestUpload([FromForm] string guestName, [FromForm] string firstFileName, [FromForm] int fileCount, [FromForm] string fileTotalSize)
        {
            if (!appConfigService.TransmitConfig.EnableGuest) { return Forbid(); }
            var uuid = requestConfirmService.CreateUploadRequest(guestName, firstFileName, fileCount, fileTotalSize);
            return Ok(new { uuid = uuid });
        }

        [HttpPost("send-message")]
        public ActionResult SendMessage([FromForm] string guestName, [FromForm] string message)
        {
            if (!appConfigService.TransmitConfig.EnableGuest) { return Forbid(); }
            requestConfirmService.CreateMessageRequest(guestName, message);
            return Ok();
        }

        [HttpPost("request-result")]
        public ActionResult RequestResult([FromForm] string uuid)
        {
            if (!appConfigService.TransmitConfig.EnableGuest) { return Forbid(); }
            var result = requestConfirmService.GetRequestResult(uuid);
            return Ok(new { result = result });
        }

        [HttpPost("upload/{uuid}")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<ActionResult> UploadFile(string uuid)
        {
            if (!appConfigService.TransmitConfig.EnableGuest) { return Forbid(); }
            var model = requestConfirmService.GetRequestModel(uuid);
            string guestPath = string.Empty;
            if (model == null)
            {
                guestPath = appConfigService.TransmitConfig.SavePath + "/guest_" + GetRemoteIp();
            }
            else
            {
                guestPath = appConfigService.TransmitConfig.SavePath + "/guest_" + model.Name;
            }
            if (!requestConfirmService.VerifyRequestUuid(uuid)) { return Unauthorized(); }
            try
            {
                // ensure upload directory exists.
                if (!Directory.Exists(guestPath))
                {
                    Directory.CreateDirectory(guestPath);
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

                    string filePath = (guestPath + "/" + fileName).Replace("/", "\\");
                    // Ensure unique file name
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var newFileName = $"{nameWithoutExt}_{counter}{ext}";
                        filePath = (guestPath + "/" + newFileName).Replace("/", "\\");
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
