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
        public bool AutoStartup { get; set; } = false;
        public bool AutoHideWindow { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "zh-CN";
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
        public int MaxFileSize { get; set; } = 4096;    // 4GB
        public string SavePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "uploads");
    }
}
