using Microsoft.Extensions.Configuration;
using QuickShare.Helpers;
using QuickShare.Models;
using Serilog;
using System.IO;
using System.Text.Json;

namespace QuickShare.Services
{
    public class AppConfigService
    {
        private readonly ILogger _logger;
        private readonly string _configFilePath;
        private readonly AppConfig _appConfig;
        private readonly IConfigurationRoot? _configuration;

        // Save configuration files in the "Config" folder under the application's base directory.
        public string ConfigFolder { get; } = Path.Combine(AppContext.BaseDirectory, "config");

        // AES encryption key for secure data handling.
        public Byte[] AesKey { get; private set; }
        public string AesKeyBase64
        {
            get { return Convert.ToBase64String(AesKey).Replace("+", "-").Replace("/", "_"); }
        }

        public UserConfig UserConfig
        {
            get => _appConfig.UserConfig;
            set { _appConfig.UserConfig = value; }
        }
        public NetworkConfig NetworkConfig
        {
            get => _appConfig.NetworkConfig;
            set { _appConfig.NetworkConfig = value; }
        }
        public TransmitConfig TransmitConfig
        {
            get => _appConfig.TransmitConfig;
            set { _appConfig.TransmitConfig = value; }
        }

        public AppConfigService(ILogger logger)
        {
            _logger = logger;
            _configFilePath = Path.Combine(ConfigFolder, "config.json");
            _appConfig = new AppConfig();

            // Load or generate AES key.
            string keyPath = "aes-key.bin";
            if (File.Exists(keyPath))
            {
                AesKey = AesKeyManager.LoadKeyFromBinary(keyPath);
            }
            else
            {
                AesKey = AesKeyManager.GenerateSecureKey();
                AesKeyManager.SaveKeyAsBinary(AesKey, keyPath);
            }

            try
            {
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                _configuration = new ConfigurationBuilder()
                    .AddJsonFile(_configFilePath)
                    .Build();

                _appConfig.UserConfig = _configuration.GetSection("UserConfig").Get<UserConfig>() ?? new UserConfig();
                _appConfig.NetworkConfig = _configuration.GetSection("NetworkConfig").Get<NetworkConfig>() ?? new NetworkConfig();
                _appConfig.TransmitConfig = _configuration.GetSection("TransmitConfig").Get<TransmitConfig>() ?? new TransmitConfig();
            }
            catch (Exception ex)
            {
                _logger.Warning($"Config file load error: {ex.Message}");
                _logger.Information("Start with the default configuration.");
                SaveConfig();
            }
        }

        /// <summary>
        /// Save the current configuration to the config file.
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                _logger.Information("Save the current configuration to the config file.");
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_appConfig, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error($"Configuration file saving failed: {ex.Message}");
            }
        }
    }
}
