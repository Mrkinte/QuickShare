using QuickShare.Views.Windows;
using Serilog;
using System.Net.Http;
using System.Xml.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace QuickShare.Services
{
    public class UpdateService(
        ILogger logger,
        ISnackbarService snackbarService,
        TranslationService trService)
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

        public void CheckUpdate()
        {
            Task.Run(async () =>
            {
                if (_isBusy)
                {
                    mSnackbarShow(
                        trService.Translate("Tip"),
                        trService.Translate("UpdateWindow_Message_Busy"),
                        ControlAppearance.Caution);
                    return;
                }
                _isBusy = true;

                // Initialize version information.
                Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
                _onlineVersion = new Version(1, 0, 0);

                bool result = await GetOnlineVersion();
                if (_onlineVersion > currentVersion)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateWindow updateDialog = new UpdateWindow(
                            _onlineVersion.ToString(),
                            _githubDownloadLink,
                            _sourceForgeDownloadLink,
                            _onlineUpdateLog,
                            trService);

                        updateDialog.ShowDialog();
                    });
                }
                else if (result)
                {
                    mSnackbarShow(
                        trService.Translate("Tip"),
                        trService.Translate("UpdateWindow_Message_Newest"),
                        ControlAppearance.Info);
                }
                _isBusy = false;
            });
        }

        private async Task<bool> GetOnlineVersion()
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                // Send a request to obtain the XML content.
                string xmlContent = string.Empty;
                mSnackbarShow(
                    trService.Translate("Tip"),
                    trService.Translate("UpdateWindow_Message_StartCheck"),
                    ControlAppearance.Info);
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
                    mSnackbarShow(
                        trService.Translate("Error"),
                        trService.Translate("UpdateWindow_Message_CheckUpdateFailed"),
                        ControlAppearance.Danger);
                    return false;
                }

                // Parse XML
                XDocument doc = XDocument.Parse(xmlContent);
                if (doc.Root == null)
                {
                    mSnackbarShow(
                        trService.Translate("Error"),
                        trService.Translate("UpdateWindow_Message_XmlParsingFailed"),
                        ControlAppearance.Danger);
                    logger.Error("Failed to parse the update file.");
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

        private void mSnackbarShow(
            string title,
            string message,
            ControlAppearance appearance)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                snackbarService.Show(
                title,
                message,
                appearance,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
            });
        }
    }
}
