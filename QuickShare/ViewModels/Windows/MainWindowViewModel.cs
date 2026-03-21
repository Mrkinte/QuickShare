using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickShare.ViewModels.Windows
{
    public partial class MainWindowViewModel(
        WebService webService,
        SqliteService sqliteService,
        AppConfigService appConfigService,
        MessageBoxService messageBoxService,
        OnlineCountService onlineCountService) : ObservableObject
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _applicationTitle = App.appName;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private int _onlineCount = 0;

        [ObservableProperty]
        private string _onlineIps = "无连接";

        [ObservableProperty]
        private string _serverStatus = "服务器（离线）";

        [ObservableProperty]
        private Brush _serverStatusColor = Brushes.Red;

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "传输",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowSort24 },
                TargetPageType = typeof(Views.Pages.TransmitPage)
            },
            new NavigationViewItem()
            {
                Content = "分享",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ShareAndroid24 },
                TargetPageType = typeof(Views.Pages.ShareRecordsPage)
            },
            new NavigationViewItem()
            {
                Content = "诊断",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Bug24 },
                TargetPageType = typeof(Views.Pages.DiagnosePage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [RelayCommand]
        public void OnLoaded()
        {
            if (!_isInitialized)
            {
                webService.ServerStatusChanged += (s, e) =>
                {
                    _ = e ? (ServerStatus = "服务器（在线）") : (ServerStatus = "服务器（离线）");
                    _ = e ? (ServerStatusColor = Brushes.Green) : (ServerStatusColor = Brushes.Red);
                };

                // 定时更新在线用户数和IP列表
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) =>
                {
                    OnlineCount = onlineCountService.GetOnlineCount();
                    if (OnlineCount <= 0)
                    {
                        OnlineIps = "无连接";
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

            if (appConfigService.UserConfig.IsFirstRun)
            {
                _ = messageBoxService.ShowMessage(
                    "提示",
                    "欢迎使用 QuickShare !\n\n" +
                    "网页端默认访问密码：quickshare\n\n" +
                    "为避免数据泄露，请及时修改默认密码（设置->传输->登录密码）。\n\n" +
                    "本软件仅支持在局域网中进行文件传输，使用时请确保各设备处于同一局域网内。\n\n" +
                    "由于本软件的SSL证书采用的是自签名证书，若第一次访问时浏览器出现风险提示，请将证书添加到信任列表。");

                // 尝试自动选择一个常用的网络接口。
                foreach (var network in NetworkHelper.GetActiveNetworkInterfaces())
                {
                    if (network.Name == "以太网")
                    {
                        appConfigService.NetworkConfig.DefaultNetwork = "以太网";
                        break;
                    }
                    else if (network.Name == "WLAN")
                    {
                        appConfigService.NetworkConfig.DefaultNetwork = "WLAN";
                        break;
                    }
                }

                // 创建默认的分类规则，如果存在旧规则就沿用。
                if (sqliteService.ReadAllSortingRules().Count == 0)
                {
                    var imageRule = new SortingRuleModel
                    {
                        SortingName = "图片",
                        SavePath = "\\图片",
                        Extension = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".svg" }
                    };
                    sqliteService.AddSortingRule(imageRule);

                    var videoRule = new SortingRuleModel
                    {
                        SortingName = "视频",
                        SavePath = "\\视频",
                        Extension = { ".mp4", ".mkv", ".mov", ".avi", ".flv", ".wmv" }
                    };
                    sqliteService.AddSortingRule(videoRule);

                    var documentRule = new SortingRuleModel
                    {
                        SortingName = "文档",
                        SavePath = "\\文档",
                        Extension = { ".txt", ".pdf", ".md", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" }
                    };
                    sqliteService.AddSortingRule(documentRule);
                }

                appConfigService.UserConfig.IsFirstRun = false;
                appConfigService.SaveConfig();
            }
        }
    }
}
