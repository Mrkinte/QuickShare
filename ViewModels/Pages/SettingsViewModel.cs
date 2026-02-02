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
        ISnackbarService snackbarService,
        AppConfigService appConfigService,
        WebService webService,
        TrayNotificationService trayNotificationService,
        UpdateService updateService,
        TranslationService trService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        /// <summary>
        /// General Settings
        /// </summary>

        [ObservableProperty]
        private bool _autoStartup = false;

        [ObservableProperty]
        private bool _autoHideWindow = false;

        [ObservableProperty]
        private string _selectedTheme = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string>? _themeItems;

        [ObservableProperty]
        private string _selectedLanguage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string>? _languageItems;

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
        private int _webPort = 0;

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
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _maxFileSizeConfirmBtnEnabled = false;

        [ObservableProperty]
        private int _maxFileSize = 0;

        [ObservableProperty]
        private string _fileSavePath = string.Empty;


        #region RelayCommand Methods

        [RelayCommand]
        private void OnAutoStartup()
        {
            try
            {
                bool result = StartupHelper.SetStartup(AutoStartup);
                if (!result)
                {
                    AutoStartup = !AutoStartup;     // The modification failed. The previous state has been restored.
                }
                else
                {
                    if (AutoStartup)
                    {
                        AutoHideWindow = true;
                    }
                    appConfigService.UserConfig.AutoStartup = AutoStartup;
                    appConfigService.UserConfig.AutoHideWindow = AutoHideWindow;
                    appConfigService.SaveConfig();
                }
            }
            catch (Exception ex)
            {
                AutoStartup = !AutoStartup;
                snackbarService.Show(
                        trService.Translate("Error"),
                        $"{trService.Translate("Settings_Message_SetAutoStartupFailed")} {ex.Message}",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
            }
        }

        [RelayCommand]
        private void OnAutoHideWindow()
        {
            appConfigService.UserConfig.AutoHideWindow = AutoHideWindow;
            appConfigService.SaveConfig();
        }

        [RelayCommand]
        private void OnChangeTheme()
        {
            if (string.IsNullOrEmpty(SelectedTheme)) return;

            if (SelectedTheme == trService.Translate("Light"))
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
        private void OnChangeLanguage()
        {
            if (string.IsNullOrEmpty(SelectedTheme)) return;

            if (SelectedLanguage == trService.Translate("zh-CN"))
            {
                appConfigService.UserConfig.Language = "zh-CN";
            }
            else
            {
                appConfigService.UserConfig.Language = "en-US";
            }
            snackbarService.Show(
                        trService.Translate("Tip"),
                        trService.Translate("Settings_Message_LanguageChanged"),
                        ControlAppearance.Info,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
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
                    snackbarService.Show(
                        trService.Translate("Error"),
                        trService.Translate("Settings_Message_PortInUse"),
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
                    return;
                }
                PortConfirmBtnEnabled = false;
                appConfigService.NetworkConfig.Port = WebPort;
                appConfigService.SaveConfig();
                snackbarService.Show(
                        trService.Translate("Tip"),
                        trService.Translate("Settings_Message_PortModifySuccessfully"),
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                snackbarService.Show(
                    trService.Translate("Error"),
                    ex.Message,
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.Tag24),
                    TimeSpan.FromSeconds(3));
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
                trService.Translate("Warrning"),
                trService.Translate("Settings_Message_PasswordInvalid1"),
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
                return;
            }

            if (Password.Length < 4 || Password.Length > 16)
            {
                snackbarService.Show(
                trService.Translate("Warrning"),
                trService.Translate("Settings_Message_PasswordInvalid2"),
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
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
                StartupHelper.SetStartup(false);    // Disable the startup auto-execution and delete the registry entries.
                trayNotificationService.DisableNotification();     // Disable the notification pop-up window.
                Directory.Delete(appConfigService.ConfigFolder, true);
                // Release the multiple locked mutexes and restart the application.
                App.MultiRunMutex!.ReleaseMutex();
                App.MultiRunMutex.Dispose();
                App.MultiRunMutex = null;
                System.Windows.Forms.Application.Restart();
                App.Current.Shutdown();
            }
            catch (Exception ex)
            {
                snackbarService.Show(
                    trService.Translate("Error"),
                    ex.Message,
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.Tag24),
                    TimeSpan.FromSeconds(3));
            }
        }

        [RelayCommand]
        private void OnCheckUpdate() => updateService.CheckUpdate();

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

            AutoStartup = appConfigService.UserConfig.AutoStartup;
            AutoHideWindow = appConfigService.UserConfig.AutoHideWindow;
            ThemeItems = new ObservableCollection<string> { trService.Translate("Light"), trService.Translate("Dark") };
            if (appConfigService.UserConfig.Theme == "Light")
            {
                SelectedTheme = trService.Translate("Light");
            }
            else
            {
                SelectedTheme = trService.Translate("Dark");
            }
            LanguageItems = new ObservableCollection<string> { trService.Translate("zh-CN"), trService.Translate("en-US") };
            if (appConfigService.UserConfig.Language == "zh-CN")
            {
                SelectedLanguage = trService.Translate("zh-CN");
            }
            else
            {
                SelectedLanguage = trService.Translate("en-US");
            }

            // Network
            WebPort = appConfigService.NetworkConfig.Port;
            webService.ServerStatusChanged += (s, e) =>
            {
                WebIsRunning = e;
                WebIsStopped = !WebIsRunning;
                if (WebIsRunning)
                {
                    snackbarService.Show(
                        trService.Translate("Tip"),
                        trService.Translate("Settings_Message_ServerRunning"),
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
                }
                else
                {
                    snackbarService.Show(
                        trService.Translate("Tip"),
                        trService.Translate("Settings_Message_ServerStopped"),
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.Tag24),
                        TimeSpan.FromSeconds(3));
                }
            };
            WebIsRunning = webService.IsRunning;
            WebIsStopped = !WebIsRunning;
            OnRefreshNetwork();

            // Transmit
            Password = appConfigService.TransmitConfig.Password;
            MaxFileSize = appConfigService.TransmitConfig.MaxFileSize;
            FileSavePath = appConfigService.TransmitConfig.SavePath;

            _isInitialized = true;
        }

        #endregion
    }
}
