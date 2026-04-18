using System.IO;

namespace QuickShare.Models
{
    public class AppConfig
    {
        public UserConfig UserConfig { get; set; } = new UserConfig();
        public NetworkConfig NetworkConfig { get; set; } = new NetworkConfig();
        public TransmitConfig TransmitConfig { get; set; } = new TransmitConfig();
    }

    public class UserConfig
    {
        public bool IsFirstRun { get; set; } = true;
        public bool AutoStartup { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public bool AutoCheckUpdate { get; set; } = true;
        public bool ExitDirectly { get; set; } = true;
        public bool DisableCloseMessage { get; set; } = false;
        public bool EnableNotificationSound { get; set; } = true;
    }

    public class NetworkConfig
    {
        public int Port { get; set; } = 53579;
        public string DefaultNetwork { get; set; } = string.Empty;
        public bool EnableMdns { get; set; } = false;
    }

    public class TransmitConfig
    {
        public string Password { get; set; } = "quickshare";
        public bool EnableGuest { get; set; } = true;
        public int RequestTimeout { get; set; } = 60;    // 响应上传请求的超时时间，超过该时间未处理则视为拒绝，单位为秒。
        public int MaxFileSize { get; set; } = 4096;    // 4GB
        public string SavePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "uploads");
        public bool AutoSorting { get; set; } = false;
    }
}
