using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;
using QuickShare.Helpers;
using QuickShare.Services;
using QuickShare.Views.Pages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class ShareViewModel(
        ISnackbarService snackbarService,
        AppConfigService appConfigService,
        INavigationService navigationService,
        ShareEditViewModel shareEditViewModel) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private long _shareId;
        private string _shareAes = string.Empty;

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

        [RelayCommand]
        private void OnExitShare() => navigationService.GoBack();

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
                "提示",
                $"复制成功：{Url}",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                TimeSpan.FromSeconds(5));
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
        private void OnEditShareRecord()
        {
            shareEditViewModel.UpdateShareRecord(_shareId);
            _ = navigationService.NavigateWithHierarchy(typeof(ShareEditPage));
        }

        public void UpdateShareRecord(long shareId)
        {
            _shareId = shareId;
            _shareAes = AesEncryptHelper.Encrypt(_shareId, appConfigService.AesKey!);

            EnableMdns = appConfigService.NetworkConfig.EnableMdns;
            JudgeConnectionMethod();
        }

        #region INavigationAware Members

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        #endregion

        #region Private Methods

        private void UpdateQrCode(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            QrCodeImage = CustomHelper.BitmapToBitmapImage(qrCode.GetGraphic(20));
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
