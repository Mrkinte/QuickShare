using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using Serilog;
using System.IO;

namespace QuickShare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController(
        ILogger logger,
        SqliteService sqliteService,
        AppConfigService appConfigService,
        DownloadTicketService downloadTicketService) : ControllerBase
    {
        /// <summary>
        /// 验证分享验证码。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost("verify/{shareId}")]
        public async Task<ActionResult> Verify(string shareId, [FromForm] string? verificationCode)
        {
            try
            {
                // 验证分享连接
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareRecord = sqliteService.ReadShareRecord(id);
                if (shareRecord.ShareId != id)
                {
                    return NotFound();
                }
                if ((verificationCode == shareRecord.VerificationCode) || (shareRecord.VerificationCode == ""))
                {
                    return Ok(new { message = "Successful" });
                }
                else
                {
                    return Unauthorized();  // 401
                }
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 获取分享基础信息。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost("info/{shareId}")]
        public async Task<IActionResult> GetShareInfo(string shareId, [FromForm] string? verificationCode)
        {
            try
            {
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareRecord = sqliteService.ReadShareRecord(id, false);
                if (shareRecord.ShareId != id)
                {
                    return NotFound();
                }
                if ((verificationCode != shareRecord.VerificationCode) && (shareRecord.VerificationCode != ""))
                {
                    return Unauthorized();  // 401
                }
                ShareProps shareProps = new ShareProps
                {
                    Description = shareRecord.Description,
                    CreateTime = shareRecord.CreateTime,
                    FileCount = shareRecord.FileCount,
                    DirectoryCount = shareRecord.DirectoryCount
                };
                return Ok(shareProps);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 获取分享文件和文件夹。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileId"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost("files/{shareId}")]
        public async Task<IActionResult> GetFiles(string shareId, [FromForm] long fileId, [FromForm] string? verificationCode)
        {
            long id;
            try
            {
                // 验证分享连接
                id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareRecord = sqliteService.ReadShareRecord(id, false);
                if (shareRecord.ShareId != id)
                {
                    return NotFound();
                }
                if ((verificationCode != shareRecord.VerificationCode) && (shareRecord.VerificationCode != ""))
                {
                    return Unauthorized();  // 401
                }
                List<FileRecordModel> fileRecords;
                if (fileId == 0)
                {
                    fileRecords = sqliteService.ReadFileRecordsByShareId(id);
                }
                else
                {
                    fileRecords = sqliteService.ReadFileRecordsByFileId(fileId);
                }
                var fileProps = new List<FileProps>();
                foreach (var record in fileRecords)
                {
                    fileProps.Add(new FileProps
                    {
                        Name = record.FileName,
                        FileId = record.FileId,
                        Extension = record.IsDirectory ? ".folder" : Path.GetExtension(record.FileName)
                    });
                }
                return Ok(fileProps);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 获取文件或文件夹的详细信息。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileId"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost("fileInfo/{shareId}")]
        public ActionResult GetFileInfo(string shareId, [FromForm] long fileId, [FromForm] string? verificationCode)
        {
            ShareRecordModel shareRecord;
            try
            {
                // 验证分享连接
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                shareRecord = sqliteService.ReadShareRecord(id, false);
                if (shareRecord.ShareId != id)
                {
                    return NotFound();
                }
            }
            catch (ArgumentException)
            {
                return NotFound();
            }

            try
            {
                if ((verificationCode != shareRecord.VerificationCode) && (shareRecord.VerificationCode != ""))
                {
                    return Unauthorized();  // 401
                }
                var filePath = sqliteService.GetFullPath(fileId);
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
                return NotFound();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return StatusCode(500, new { error = "Failed to get file info." });
            }
        }

        /// <summary>
        /// 获取单次下载的ticket，验证分享链接和验证码后返回一个唯一的ticket，下载接口需要携带这个ticket以防止暴力破解下载链接。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileId"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost("ticket/{shareId}")]
        public ActionResult GetDownloadTicket(string shareId, [FromForm] long fileId, [FromForm] string? verificationCode)
        {
            try
            {
                // 验证分享连接
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
                var shareRecord = sqliteService.ReadShareRecord(id, false);
                if (shareRecord.ShareId != id)
                {
                    return NotFound();
                }
                if ((verificationCode != shareRecord.VerificationCode) && (shareRecord.VerificationCode != ""))
                {
                    return Unauthorized();  // 401
                }
                return Ok(new { ticket = downloadTicketService.GenericDownloadTicket(fileId) });
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 下载文件。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        [HttpGet("download/{shareId}/{fileId}/{ticket}")]
        public async Task<IActionResult> DownloadFile(string shareId, long fileId, string ticket)
        {
            try
            {
                var id = AesEncryptHelper.Decrypt(shareId, appConfigService.AesKey);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }

            try
            {
                if (!downloadTicketService.VerifyDownloadTicket(fileId, ticket))
                {
                    return Unauthorized();  // 401
                }
                var fileRecord = sqliteService.ReadFileRecord(fileId);
                if (fileRecord.IsDirectory)
                {
                    return StatusCode(500, new { error = "Do not support downloading folders." });
                }
                var filePath = sqliteService.GetFullPath(fileRecord.FileId);
                var fileProvider = new FileExtensionContentTypeProvider();
                if (!fileProvider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }
                sqliteService.IncrementDownloadCount(fileId);
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return StatusCode(500, new { error = "File download failed." });
            }
        }
    }
}
