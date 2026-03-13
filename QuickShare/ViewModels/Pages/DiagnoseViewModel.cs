using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickShare.Helpers;
using QuickShare.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Abstractions.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class DiagnoseViewModel(
        WebService webService,
        AppConfigService appConfigService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private static readonly Brush _defaultBrush =
            Application.Current.FindResource("TextFillColorPrimaryBrush") as Brush ?? Brushes.Gray;

        [ObservableProperty]
        private string _webServerDiagnoseStatus = "未诊断";

        [ObservableProperty]
        private string _webServerDiagnoseDetail = "未诊断";

        [ObservableProperty]
        private Brush _webServerDiagnoseColor = _defaultBrush;

        [ObservableProperty]
        private string _firewallDiagnoseStatus = "未诊断";

        [ObservableProperty]
        private string _firewallDiagnoseDetail = "未诊断";

        [ObservableProperty]
        private Brush _firewallDiagnoseColor = _defaultBrush;

        [ObservableProperty]
        private Visibility _addFirewallRuleBtnVisiblity = Visibility.Collapsed;

        [RelayCommand]
        private async Task OnAddApplicationRule()
        {
            if (FirewallHelper.IsApplicationAllowed()) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{Path.Combine(AppContext.BaseDirectory, "repaire-firewall-rule.bat")}\"",
            })?.WaitForExit();
        }

        [RelayCommand]
        private void OnDiagnose()
        {
            DiagnoseWebServerStatus();
            DiagnoseFirewallStatus();
        }

        private void DiagnoseWebServerStatus()
        {
            if (!webService.IsRunning)
            {
                WebServerDiagnoseStatus = "异常";
                if (NetworkHelper.IsPortInUse(appConfigService.NetworkConfig.Port))
                {
                    WebServerDiagnoseDetail = "服务器未启动，端口被占用。";
                }
                else
                {
                    WebServerDiagnoseDetail = "服务器未启动，请在设置页面尝试启动服务器。";
                }
                WebServerDiagnoseColor = Brushes.Red;
                return;
            }
            WebServerDiagnoseStatus = "正常";
            WebServerDiagnoseDetail = "服务器已启动，未发现异常。";
            WebServerDiagnoseColor = Brushes.Green;
        }

        private void DiagnoseFirewallStatus()
        {
            if (!FirewallHelper.IsApplicationAllowed())
            {
                FirewallDiagnoseStatus = "异常";
                FirewallDiagnoseDetail = "防火墙配置异常，未放行，请手动放行或尝试自动修复。";
                FirewallDiagnoseColor = Brushes.Red;
                AddFirewallRuleBtnVisiblity = Visibility.Visible;
                return;
            }
            FirewallDiagnoseStatus = "正常";
            FirewallDiagnoseDetail = "防火墙配置正常，已放行。";
            FirewallDiagnoseColor = Brushes.Green;
            AddFirewallRuleBtnVisiblity = Visibility.Collapsed;
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
    }
}
