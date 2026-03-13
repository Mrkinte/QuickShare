using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickShare.Helpers;
using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class SettingsViewModel(
        WebService webService,
        UpdateService updateService,
        ISnackbarService snackbarService,
        AppConfigService appConfigService,
        MessageBoxService messageBoxService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        /// <summary>
        /// General Settings
        /// </summary>

        [ObservableProperty]
        private bool _autoStartup = appConfigService.UserConfig.AutoStartup;

        [ObservableProperty]
        private string _selectedTheme = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _themeItems = new ObservableCollection<string> { "亮色", "暗色" };

        [ObservableProperty]
        private string _selectedLanguage = string.Empty;

        [ObservableProperty]
        private bool _autoCheckUpdate = appConfigService.UserConfig.AutoCheckUpdate;

        [ObservableProperty]
        private bool _disableCloseMessage = appConfigService.UserConfig.DisableCloseMessage;

        /// <summary>
        /// Network Settings
        /// </summary>

        [ObservableProperty]
        private bool _webIsRunning = false;

        [ObservableProperty]
        private bool _webIsStopped = false;

        [ObservableProperty]
        private bool _portConfirmBtnEnabled = false;

        [ObservableProperty]
        private int _webPort = appConfigService.NetworkConfig.Port;

        [ObservableProperty]
        private NetworkInterfaceInfo? _selectedNetwork;

        [ObservableProperty]
        private ObservableCollection<NetworkInterfaceInfo>? _activedNetworks;

        [ObservableProperty]
        private bool _enableHttps = false;

        /// <summary>
        /// Transmit Settings
        /// </summary>

        [ObservableProperty]
        private bool _passwordConfirmBtnEnabled = false;

        [ObservableProperty]
        private string _password = appConfigService.TransmitConfig.Password;

        [ObservableProperty]
        private bool _maxFileSizeConfirmBtnEnabled = false;

        [ObservableProperty]
        private int _maxFileSize = appConfigService.TransmitConfig.MaxFileSize;

        [ObservableProperty]
        private string _fileSavePath = appConfigService.TransmitConfig.SavePath;

        #region RelayCommand Methods

        [RelayCommand]
        private void OnAutoStartup()
        {
            try
            {
                bool result = StartupHelper.SetStartup(AutoStartup);
                if (!result)
                {
                    AutoStartup = !AutoStartup;
                }
                else
                {
                    appConfigService.UserConfig.AutoStartup = AutoStartup;
                    appConfigService.SaveConfig();
                }
            }
            catch (Exception ex)
            {
                AutoStartup = !AutoStartup;
                _ = messageBoxService.ShowMessage("错误", $"设置开机自启动失败：{ex.Message}");
            }
        }

        [RelayCommand]
        private void OnChangeTheme()
        {
            if (string.IsNullOrEmpty(SelectedTheme)) return;

            if (SelectedTheme == "亮色")
            {
                appConfigService.UserConfig.Theme = "Light";
                if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light)
                {
                    return;
                }
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                appConfigService.SaveConfig();
            }
            else
            {

                appConfigService.UserConfig.Theme = "Dark";
                if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
                {
                    return;
                }
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                appConfigService.SaveConfig();
            }
        }

        [RelayCommand]
        private void OnAutoCheckUpdate()
        {
            appConfigService.UserConfig.AutoCheckUpdate = AutoCheckUpdate;
            appConfigService.SaveConfig();
        }

        [RelayCommand]
        private void OnDisableCloseMessage()
        {
            appConfigService.UserConfig.DisableCloseMessage = DisableCloseMessage;
            appConfigService.SaveConfig();
        }

        [RelayCommand]
        private async Task OnStartWebServer()
        {
            await webService.StartWebServer();
        }

        [RelayCommand]
        private void OnStopWebServer()
        {
            webService?.StopWebServer();
        }

        [RelayCommand]
        private void OnWebPortChanged()
        {
            if (PortConfirmBtnEnabled == false)
            {
                PortConfirmBtnEnabled = true;
            }
        }

        [RelayCommand]
        private void OnModifyWebPort()
        {
            try
            {
                if (NetworkHelper.IsPortInUse(WebPort))
                {
                    _ = messageBoxService.ShowMessage("错误", $"端口{WebPort}被占用。");
                    return;
                }
                PortConfirmBtnEnabled = false;
                appConfigService.NetworkConfig.Port = WebPort;
                appConfigService.SaveConfig();
                snackbarService.Show(
                    "提示",
                    "端口修改成功。",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _ = messageBoxService.ShowMessage("错误", ex.Message);
            }
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

        partial void OnSelectedNetworkChanged(NetworkInterfaceInfo? oldValue, NetworkInterfaceInfo? newValue)
        {
            if (!_isInitialized || (oldValue == null)) return;

            if (newValue != null)
            {
                if (oldValue.Name != newValue.Name)
                {
                    appConfigService.NetworkConfig.DefaultNetwork = newValue.Name;
                    appConfigService.SaveConfig();
                }
            }
        }

        [RelayCommand]
        private void OnPasswordChanged()
        {
            if (PasswordConfirmBtnEnabled == false)
            {
                PasswordConfirmBtnEnabled = true;
            }
        }

        [RelayCommand]
        private void OnResetPassword()
        {
            if (Regex.IsMatch(Password, @"[^a-zA-Z0-9]"))
            {
                snackbarService.Show(
                    "警告",
                    "密码无效，仅支持字母和数字。",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5));
                return;
            }

            if (Password.Length < 4 || Password.Length > 16)
            {
                snackbarService.Show(
                    "警告",
                    "密码无效，仅支持4-16个字符长度。",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5));
                return;
            }
            PasswordConfirmBtnEnabled = false;
            appConfigService.TransmitConfig.Password = Password;
            appConfigService.SaveConfig();
        }

        [RelayCommand]
        private void OnMaxFileSizeChanged()
        {
            if (MaxFileSizeConfirmBtnEnabled == false)
            {
                MaxFileSizeConfirmBtnEnabled = true;
            }
        }

        [RelayCommand]
        private void OnModifyMaxFileSize()
        {
            appConfigService.TransmitConfig.MaxFileSize = MaxFileSize;
            appConfigService.SaveConfig();
            MaxFileSizeConfirmBtnEnabled = false;
        }

        [RelayCommand]
        private void OnSelectFileSavePath()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                FileSavePath = folderBrowserDialog.SelectedPath;
                appConfigService.TransmitConfig.SavePath = FileSavePath;
                appConfigService.SaveConfig();
            }
        }

        [RelayCommand]
        private void OnOpenLogFolder()
        {
            if (Directory.Exists(App.LogFolder))
            {
                Process.Start("explorer.exe", App.LogFolder);
            }
        }

        [RelayCommand]
        private void OnRestoreDefault()
        {
            try
            {
                StartupHelper.SetStartup(false);    // 禁用开机自启动，并删除注册表项。
                Directory.Delete(appConfigService.ConfigFolder, true);
                // 释放所有被锁定的互斥锁，并重新启动应用程序。
                App.MultiRunMutex!.ReleaseMutex();
                App.MultiRunMutex.Dispose();
                App.MultiRunMutex = null;
                System.Windows.Forms.Application.Restart();
                App.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _ = messageBoxService.ShowMessage("错误", ex.Message);
            }
        }

        [RelayCommand]
        private async Task OnCheckUpdate() => await updateService.CheckUpdate();

        #endregion


        #region INavigationAware Methods

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = $"© 版权所有 2026, Mrkinte, 当前版本 {version!.Major}.{version.Minor}.{version.Build}";

            if (appConfigService.UserConfig.Theme == "Light")
            {
                SelectedTheme = "亮色";
            }
            else
            {
                SelectedTheme = "暗色";
            }

            // Network
            webService.ServerStatusChanged += (s, e) =>
            {
                WebIsRunning = e;
                WebIsStopped = !WebIsRunning;
                if (WebIsRunning)
                {
                    snackbarService.Show(
                        "提示",
                        "服务器开始运行。",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(5));
                }
                else
                {
                    snackbarService.Show(
                        "提示",
                        "服务器已停止运行。",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(5));
                }
            };
            WebIsRunning = webService.IsRunning;
            WebIsStopped = !WebIsRunning;
            OnRefreshNetwork();

            _isInitialized = true;
        }

        #endregion
    }
}
