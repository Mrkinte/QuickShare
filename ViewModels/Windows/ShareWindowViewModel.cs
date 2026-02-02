using QRCoder;
using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace QuickShare.ViewModels.Windows
{
    public partial class ShareWindowViewModel(
            ShareModel share,
            AppConfigService appConfigService,
            ISnackbarService snackbarService,
            ShareEditWindowManage shareEditWindowManage,
            TranslationService trService) : ObservableObject
    {
        private string _shareAes = AesEncryptHelper.Encrypt(share.Id, appConfigService.AesKey!);

        [ObservableProperty]
        private BitmapImage? _qrCodeImage;

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private bool _enableMdns;

        [ObservableProperty]
        private ObservableCollection<NetworkInterfaceInfo>? _activedNetworks;

        [ObservableProperty]
        private NetworkInterfaceInfo? _selectedNetwork;

        partial void OnSelectedNetworkChanged(NetworkInterfaceInfo? value)
        {
            if (value == null || value.IpAddress == null)
            {
                return;
            }
            Url = $"https://{value.IpAddress.ToString()}:{appConfigService.NetworkConfig.Port}/share/{_shareAes}";
            UpdateQrCode(Url);
        }

        [RelayCommand]
        private void OnCopyUrl()
        {
            Clipboard.SetText(Url);
            snackbarService.Show(
                trService.Translate("Tip"),
                $"{trService.Translate("ShareWindow_Message_CopyUrlSuccess")} {Url}",
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
            JudgeConnectionMethod();
            appConfigService.NetworkConfig.EnableMdns = EnableMdns;
            appConfigService.SaveConfig();
        }

        [RelayCommand]
        private void OnOpenEditDialog()
        {
            shareEditWindowManage.CreateShareEditWindow(share);
        }

        [RelayCommand]
        private void OnLoaded()
        {
            EnableMdns = appConfigService.NetworkConfig.EnableMdns;
            JudgeConnectionMethod();
        }

        #region private methods

        private void UpdateQrCode(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            QrCodeImage = ImageHelper.BitmapToBitmapImage(qrCode.GetGraphic(20));
        }

        private void JudgeConnectionMethod()
        {
            if (EnableMdns)
            {
                Url = $"https://{Environment.MachineName.ToLower()}:{appConfigService.NetworkConfig.Port}/share/{_shareAes}";
                UpdateQrCode(Url);
            }
            else
            {
                OnRefreshNetwork();
            }
        }

        #endregion
    }
}
