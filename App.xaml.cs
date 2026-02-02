using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickShare.Helpers;
using QuickShare.Services;
using QuickShare.ViewModels.Pages;
using QuickShare.ViewModels.Windows;
using QuickShare.Views.Pages;
using QuickShare.Views.Windows;
using Serilog;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace QuickShare
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private const int WM_COPYDATA = 0x004A;

        private const string appName = "Quick Share";
        public static Mutex? MultiRunMutex { get; set; } = null;
        public static string LogFolder { get; } = Path.Combine(AppContext.BaseDirectory, "logs");

        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Snackbar manipulation
                services.AddSingleton<ISnackbarService, SnackbarService>();

                // ContentDialog manipulation
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Service
                services.AddSingleton<WebService>();
                services.AddSingleton<SendToService>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<SqliteService>();
                services.AddSingleton<AppConfigService>();
                services.AddSingleton<ShareWindowManage>();
                services.AddSingleton<TranslationService>();
                services.AddSingleton<CertificateService>();
                services.AddSingleton<OnlineCountService>();
                services.AddSingleton<ShareEditWindowManage>();
                services.AddSingleton<TrayNotificationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<TransmitPage>();
                services.AddSingleton<TransmitViewModel>();
                services.AddSingleton<SharePage>();
                services.AddSingleton<ShareViewModel>();
                services.AddSingleton<DiagnosePage>();
                services.AddSingleton<DiagnoseViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: Path.Combine(LogFolder, "app-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        fileSizeLimitBytes: null,
                        rollOnFileSizeLimit: false,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                logging.Services.AddSingleton(Log.Logger);
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // Software multi-instance check
            bool createdNew;

            MultiRunMutex = new Mutex(true, appName, out createdNew);

            // Language
            var trService = Services.GetRequiredService<TranslationService>();
            var appConfigService = Services.GetRequiredService<AppConfigService>();
            if (appConfigService.UserConfig.Language == "en-US")
            {
                trService.SwitchLanguage("en-US");
            }

            if (!createdNew)
            {
                if (e.Args.Length > 0 && SendFilesToRunningInstance(e.Args))
                {
                    Current.Shutdown();
                    return;
                }
                MessageBox.Show(trService.Translate("MainWindow_Message_AppIsRunning"),
                    trService.Translate("Tip"), MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            await _host.StartAsync();

            // Start web server
            await Services.GetRequiredService<WebService>().StartWebServer();

            if (e.Args.Length > 0)
            {
                MainWindow.Hide();
                var sendToService = Services.GetRequiredService<SendToService>();
                sendToService?.ProcessSendToFiles(e.Args);
            }

            ShortcutHelper.EnsureQuickShareShortcutInSendTo(Path.Combine(AppContext.BaseDirectory, "Quick Share.exe"));
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await Services.GetRequiredService<WebService>().StopWebServer();

            Services.GetRequiredService<SqliteService>().Close();

            await _host.StopAsync();
            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
            Log.Logger.Error(e.Exception, "Unhandled exception occurred in application");
            e.Handled = true;
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease check the log file for more details.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private bool SendFilesToRunningInstance(string[] files)
        {
            try
            {
                IntPtr hWnd = FindWindow(null, appName);

                if (hWnd == IntPtr.Zero)
                    return false;

                string data = string.Join("\n", files);
                byte[] buffer = Encoding.Unicode.GetBytes(data);
                var cds = new COPYDATASTRUCT
                {
                    dwData = IntPtr.Zero,
                    cbData = buffer.Length,
                    lpData = Marshal.AllocHGlobal(buffer.Length)
                };

                Marshal.Copy(buffer, 0, cds.lpData, buffer.Length);

                try
                {
                    SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ref cds);
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(cds.lpData);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
