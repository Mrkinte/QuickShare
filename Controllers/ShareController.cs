using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using QuickShare.Helpers;
using QuickShare.Services;
using System.IO;

namespace QuickShare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController(
        SqliteService sqliteService,
        AppConfigService appConfigService) : ControllerBase
    {
        [HttpGet("is_private/{shareId}")]
        public async Task<IActionResult> IsPrivate(string shareId)
        {
            try
            {
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareHistory = sqliteService.ReadShareHistory(id);
                if (shareHistory == null)
                {
                    return NotFound(new { error = "Invalid sharing link." });
                }
                return Ok(new
                {
                    isPrivate = !string.IsNullOrEmpty(shareHistory.VerifyCode)
                });
            }
            catch
            {
                return NotFound(new { error = "Invalid sharing link." });
            }
        }

        [HttpPost("info/{shareId}")]
        public async Task<IActionResult> GetShareInfo(string shareId, [FromForm] string? verifyCode)
        {
            try
            {
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareHistory = sqliteService.ReadShareHistory(id);
                if (shareHistory == null)
                {
                    return NotFound(new { error = "Invalid sharing link." });
                }
                if (!string.IsNullOrEmpty(shareHistory.VerifyCode) &&
                    shareHistory.VerifyCode != verifyCode)
                {
                    // 401 Unauthorized
                    return Unauthorized(new { error = "Incorrect verification code." });
                }

                return Ok(shareHistory);
            }
            catch
            {
                return NotFound(new { error = "Invalid sharing link." });
            }
        }

        /// <summary>
        /// Download shared file.
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        [HttpGet("download/{shareId}/{fileId}")]
        public async Task<IActionResult> DownloadFile(string shareId, long fileId)
        {
            try
            {
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
            }
            catch
            {
                return NotFound(new { error = "Invalid sharing link." });
            }

            try
            {
                var path = sqliteService.ReadFilePath(fileId);
                if (path == null)
                    return StatusCode(500, new { error = "File download failed." });

                if (!System.IO.File.Exists(path))
                    return StatusCode(500, new { error = "File download failed." });

                var fileProvider = new FileExtensionContentTypeProvider();
                if (!fileProvider.TryGetContentType(path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                sqliteService.IncrementDownloadCount(fileId);
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                return File(fileStream, contentType, Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "File download failed.", message = ex.Message });
            }
        }
    }
}
