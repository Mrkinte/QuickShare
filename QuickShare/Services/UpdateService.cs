using QuickShare.Views.Windows;
using Serilog;
using System.Net.Http;
using System.Windows;
using System.Xml.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace QuickShare.Services
{
    public class UpdateService(
        ILogger logger,
        ISnackbarService snackbarService,
        MessageBoxService messageBoxService)
    {
        // Update source
        private readonly List<string> _urlList = new List<string> {
            "https://sourceforge.net/p/quick-share/code/ci/main/tree/version.xml?format=raw",
            "https://raw.githubusercontent.com/Mrkinte/QuickShare/refs/heads/main/version.xml"};

        private bool _isBusy = false;
        private Version _onlineVersion = new Version(1, 0, 0);
        private string _githubDownloadLink = string.Empty;
        private string _sourceForgeDownloadLink = string.Empty;
        private string _onlineUpdateLog = string.Empty;

        /// <summary>
        /// 检查更新，启用自动检查更新时，showMessage = false，隐藏提示，仅发现新版本时弹窗。
        /// </summary>
        /// <param name="showMessage"></param>
        /// <returns></returns>
        public async Task CheckUpdate(bool showMessage = true)
        {
            if (_isBusy)
            {
                snackbarService.Show(
                    "提示",
                    "正在检查新版本中，请耐心等待。",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    TimeSpan.FromSeconds(5));
                return;
            }
            _isBusy = true;

            // Initialize version information.
            Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
            _onlineVersion = new Version(1, 0, 0);

            if (showMessage)
            {
                snackbarService.Show(
                    "提示",
                    "开始检查新版本。",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    TimeSpan.FromSeconds(5));
            }
            bool result = await GetOnlineVersion(showMessage);
            if (_onlineVersion > currentVersion)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateWindow updateDialog = new UpdateWindow(
                        _onlineVersion.ToString(),
                        _githubDownloadLink,
                        _sourceForgeDownloadLink,
                        _onlineUpdateLog);

                    updateDialog.ShowDialog();
                });
            }
            else if (result)
            {
                if (showMessage)
                {
                    snackbarService.Show(
                        "提示",
                        "当前已是最新版本。",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(5));
                }
            }
            _isBusy = false;
        }

        private async Task<bool> GetOnlineVersion(bool showMessage)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                // Send a request to obtain the XML content.
                string xmlContent = string.Empty;
                foreach (var url in _urlList)
                {
                    try
                    {
                        logger.Information("Checking for updates...");
                        logger.Information($"Update source: {url}");
                        xmlContent = await client.GetStringAsync(url);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Current source acquisition failed: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(xmlContent))
                {
                    if (!showMessage)
                    {
                        _ = messageBoxService.ShowMessage(
                            "错误",
                            "检查更新失败，请检查网络连接或稍后再试。");
                    }
                    return false;
                }

                // Parse XML
                XDocument doc = XDocument.Parse(xmlContent);
                if (doc.Root == null)
                {
                    logger.Error("Failed to parse the update file.");
                    if (showMessage)
                    {
                        _ = messageBoxService.ShowMessage(
                            "错误",
                            "更新文件解析失败。");
                    }
                    return false;
                }

                var version = doc.Root.Element("version")?.Value;
                if (version != null)
                {
                    _onlineVersion = new Version(version);
                }
                _githubDownloadLink = doc.Root.Element("download_link")?.Value ?? string.Empty;
                _sourceForgeDownloadLink = doc.Root.Element("download_link2")?.Value ?? string.Empty;
                _onlineUpdateLog = doc.Root.Element("update_contents")?.Value ?? string.Empty;
                logger.Information($"Update obtained successfully. Latest version: {_onlineVersion}");
                return true;
            }
        }
    }
}
