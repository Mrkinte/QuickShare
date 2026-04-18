using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickShare.Services;

namespace QuickShare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController(
        AppConfigService appConfigService,
        OnlineCountService onlineCountService) : ControllerBase
    {
        /// <summary>
        /// 连接心跳，统计在线数。
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        [HttpGet("alive/{uuid}")]
        public async Task<ActionResult> Alive(string uuid)
        {
            var actualIp = GetRemoteIp();
            if (string.IsNullOrWhiteSpace(uuid))
                return BadRequest("Uuid is required");
            onlineCountService.UpdateVisitorActivity(string.IsNullOrEmpty(actualIp) ? uuid : actualIp);
            return Ok(new { status = "alive" });
        }

        /// <summary>
        /// 获取上传参数。
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("parameter")]
        public IActionResult GetParameters()
        {
            return Ok(new
            {
                maxFileSize = appConfigService.TransmitConfig.MaxFileSize,
                autoSorting = appConfigService.TransmitConfig.AutoSorting,
                enableGuest = appConfigService.TransmitConfig.EnableGuest,
                requestTimeout = appConfigService.TransmitConfig.RequestTimeout,
            });
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
