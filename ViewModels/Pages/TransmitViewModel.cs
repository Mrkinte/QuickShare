using QRCoder;
using QuickShare.Helpers;
using QuickShare.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace QuickShare.ViewModels.Pages
{
    public partial class TransmitViewModel(
        ILogger logger,
        AppConfigService appConfigService,
        ISnackbarService snackbarService,
        TranslationService trService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private BitmapImage? _qrCodeImage;

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private bool _enableMdns = appConfigService.NetworkConfig.EnableMdns;

        [ObservableProperty]
        private ObservableCollection<NetworkInterfaceInfo>? _activedNetworks;

        [ObservableProperty]
        private NetworkInterfaceInfo? _selectedNetwork;

        [RelayCommand]
        private void OnOpenUploadFolder()
        {
            if (Directory.Exists(appConfigService.TransmitConfig.SavePath))
            {
                Process.Start("explorer.exe", appConfigService.TransmitConfig.SavePath);
            }
        }

        partial void OnSelectedNetworkChanged(NetworkInterfaceInfo? value)
        {
            if (value == null || value.IpAddress == null)
            {
                return;
            }
            Url = $"https://{value.IpAddress.ToString()}:{appConfigService.NetworkConfig.Port}";
            UpdateQrCode(Url);
        }

        [RelayCommand]
        private void OnCopyUrl()
        {
            Clipboard.SetText(Url);
            snackbarService.Show(
                            trService.Translate("Tip"),
                            $"{trService.Translate("Transmit_Message_CopyUrlSuccess")} {Url}",
                            ControlAppearance.Success,
                            new SymbolIcon(SymbolRegular.Tag24),
                            TimeSpan.FromSeconds(3));
        }

        [RelayCommand]
        private void OnRefreshNetwork()
        {
            ActivedNetworks = new ObservableCollection<NetworkInterfaceInfo>(
                NetworkHelper.GetActiveNetworkInterfaces());

            var network = new NetworkInterfaceInfo();
            foreach (NetworkInterfaceInfo networkInterface in ActivedNetworks)
            {
                if (networkInterface.Name == appConfigService.NetworkConfig.DefaultNetwork)
                {
                    network = networkInterface;
                    break;
                }
                else
                {
                    network = ActivedNetworks.FirstOrDefault();
                }
            }
            SelectedNetwork = network;
        }

        [RelayCommand]
        private void OnEnableMdns()
        {
            if (EnableMdns)
            {
                Url = $"https://{Environment.MachineName.ToLower()}.local:{appConfigService.NetworkConfig.Port}";
                UpdateQrCode(Url);
            }
            else
            {
                OnRefreshNetwork();
            }
            appConfigService.NetworkConfig.EnableMdns = EnableMdns;
            appConfigService.SaveConfig();
        }

        #region INavigationAware Members

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            EnableMdns = appConfigService.NetworkConfig.EnableMdns;
            if (EnableMdns)
            {
                Url = $"https://{Environment.MachineName.ToLower()}.local:{appConfigService.NetworkConfig.Port}";
                UpdateQrCode(Url);
            }
            else
            {
                OnRefreshNetwork();
            }
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            // ensure upload directory exists.
            if (!Directory.Exists(appConfigService.TransmitConfig.SavePath))
            {
                try
                {
                    Directory.CreateDirectory(appConfigService.TransmitConfig.SavePath);
                }
                catch (Exception ex)
                {
                    logger.Error("Create upload folder failed: {0}", ex.Message);
                    snackbarService.Show(
                            trService.Translate("Error"),
                            $"{trService.Translate("Transmit_Message_CreateUploadFolderFailed")} {ex.Message}",
                            ControlAppearance.Danger,
                            new SymbolIcon(SymbolRegular.ErrorCircle24),
                            TimeSpan.FromSeconds(5));
                }
            }
            _isInitialized = true;
        }

        #endregion

        private void UpdateQrCode(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            QrCodeImage = ImageHelper.BitmapToBitmapImage(qrCode.GetGraphic(20));
        }
    }
}
