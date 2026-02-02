using QuickShare.Helpers;
using QuickShare.Services;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Wpf.Ui.Abstractions.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class DiagnoseViewModel(
        WebService webService,
        TranslationService trService,
        AppConfigService appConfigService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private static readonly Brush _defaultBrush =
            Application.Current.FindResource("TextFillColorPrimaryBrush") as Brush ?? Brushes.Gray;

        [ObservableProperty]
        private string _webServerDiagnoseStatus = trService.Translate("Diagnose_Text_WaitDiagnose");

        [ObservableProperty]
        private string _webServerDiagnoseDetail = trService.Translate("Diagnose_Text_WaitDiagnose");

        [ObservableProperty]
        private Brush _webServerDiagnoseColor = _defaultBrush;

        [ObservableProperty]
        private string _firewallDiagnoseStatus = trService.Translate("Diagnose_Text_WaitDiagnose");

        [ObservableProperty]
        private string _firewallDiagnoseDetail = trService.Translate("Diagnose_Text_WaitDiagnose");

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
                Arguments = $"/c \"{Path.Combine(AppContext.BaseDirectory, "add-firewall-rule.bat")}\"",
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
                WebServerDiagnoseStatus = trService.Translate("Diagnose_Text_Abnormal");
                if (NetworkHelper.IsPortInUse(appConfigService.NetworkConfig.Port))
                {
                    WebServerDiagnoseDetail = trService.Translate("Diagnose_Text_ServerDetail1");
                }
                else
                {
                    WebServerDiagnoseDetail = trService.Translate("Diagnose_Text_ServerDetail2");
                }
                WebServerDiagnoseColor = Brushes.Red;
                return;
            }
            WebServerDiagnoseStatus = trService.Translate("Diagnose_Text_Normal");
            WebServerDiagnoseDetail = trService.Translate("Diagnose_Text_ServerDetail3");
            WebServerDiagnoseColor = Brushes.Green;
        }

        private void DiagnoseFirewallStatus()
        {
            if (!FirewallHelper.IsApplicationAllowed())
            {
                FirewallDiagnoseStatus = trService.Translate("Diagnose_Text_Abnormal");
                FirewallDiagnoseDetail = trService.Translate("Diagnose_Text_FirewallDetail1");
                FirewallDiagnoseColor = Brushes.Red;
                AddFirewallRuleBtnVisiblity = Visibility.Visible;
                return;
            }
            FirewallDiagnoseStatus = trService.Translate("Diagnose_Text_Normal");
            FirewallDiagnoseDetail = trService.Translate("Diagnose_Text_FirewallDetail2");
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
