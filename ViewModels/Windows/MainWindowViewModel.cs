using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickShare.ViewModels.Windows
{
    public partial class MainWindowViewModel(
        AppConfigService appConfigService,
        WebService webService,
        OnlineCountService onlineCountService,
        TranslationService trService) : ObservableObject
    {
        private bool _firstStartup = true;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _applicationTitle = trService.Translate("AppName");

        [ObservableProperty]
        private int _onlineCount = 0;

        [ObservableProperty]
        private string _onlineIps = trService.Translate("MainWindow_ToolTip_NoConnections");

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = trService.Translate("Transmit_Text_Title"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowSort24 },
                TargetPageType = typeof(Views.Pages.TransmitPage)
            },
            new NavigationViewItem()
            {
                Content = trService.Translate("Share_Text_Title"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.ShareAndroid24 },
                TargetPageType = typeof(Views.Pages.SharePage)
            },
            new NavigationViewItem()
            {
                Content = trService.Translate("Diagnose_Text_Title"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Bug24 },
                TargetPageType = typeof(Views.Pages.DiagnosePage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = trService.Translate("Settings_Text_Title"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private string _serverStatus = trService.Translate("MainWindow_Text_Offline");

        [ObservableProperty]
        private Brush _serverStatusColor = Brushes.Red;

        [RelayCommand]
        public void OnLoaded()
        {
            if (!_isInitialized)
            {
                webService.ServerStatusChanged += (s, e) =>
                {
                    _ = e ? (ServerStatus = trService.Translate("MainWindow_Text_Online")) :
                    (ServerStatus = trService.Translate("MainWindow_Text_Offline"));
                    _ = e ? (ServerStatusColor = Brushes.Green) : (ServerStatusColor = Brushes.Red);
                };
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) =>
                {
                    OnlineCount = onlineCountService.GetOnlineCount();
                    if (OnlineCount <= 0)
                    {
                        OnlineIps = trService.Translate("MainWindow_ToolTip_NoConnections");
                        return;
                    }
                    OnlineIps = string.Join("\n", onlineCountService.GetOnlineIps());
                };
                timer.Start();
                _isInitialized = true;
            }

            switch (appConfigService.UserConfig.Theme)
            {
                case "Light":
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    break;

                default:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    break;
            }
        }

        [RelayCommand]
        public void OnWindowActivated()
        {
            if (_firstStartup && appConfigService.UserConfig.AutoHideWindow)
            {
                App.Current.MainWindow.Hide();
                _firstStartup = false;
            }
        }
    }
}
