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

        public static string appName = "QuickShare";
        public static Mutex? MultiRunMutex { get; set; } = null;
        public static string LogFolder { get; } = Path.Combine(AppContext.BaseDirectory, "logs");

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            // .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Services
                services.AddSingleton<WebService>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<SqliteService>();
                services.AddSingleton<AppConfigService>();
                services.AddSingleton<MessageBoxService>();
                services.AddSingleton<CertificateService>();
                services.AddSingleton<OnlineCountService>();
                services.AddSingleton<DownloadTicketService>();

                // Windows and Pages
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<TransmitPage>();
                services.AddSingleton<TransmitViewModel>();
                services.AddSingleton<ShareRecordsPage>();
                services.AddSingleton<ShareRecordsViewModel>();
                services.AddSingleton<ShareEditPage>();
                services.AddSingleton<ShareEditViewModel>();
                services.AddSingleton<SharePage>();
                services.AddSingleton<ShareViewModel>();
                services.AddSingleton<DiagnosePage>();
                services.AddSingleton<DiagnoseViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<AutomaticSortingEditPage>();
                services.AddSingleton<AutomaticSortingEditViewModel>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                Log.Logger = new LoggerConfiguration()
#if DEBUG
                    .WriteTo.Console()
#endif
                    .WriteTo.File(
                        path: Path.Combine(LogFolder, ".txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        fileSizeLimitBytes: null,
                        rollOnFileSizeLimit: false,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(5))
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
        /// 后台有任务正在进行，阻止用户操作窗口。
        /// </summary>
        /// <param name="busy"></param>
        public static void SetBusy(bool busy)
        {
            var mainWindowViewModel = App.Services.GetRequiredService<MainWindowViewModel>();
            mainWindowViewModel.IsBusy = busy;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // Software multi-instance check
            bool createdNew;

            MultiRunMutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                if (e.Args.Length > 0 && SendFilesToRunningInstance(e.Args))
                {
                    Current.Shutdown();
                    return;
                }
                _ = Services.GetRequiredService<MessageBoxService>().ShowMessage(
                    "提示",
                    "已有一个软件在运行中！");
                Current.Shutdown();
                return;
            }

            await _host.StartAsync();

            // Start web server
            await Services.GetRequiredService<WebService>().StartWebServer();

            if (e.Args.Length > 0)
            {
                App.SetBusy(true);
                await SendToHelper.ProcessSendToFilesAsync(e.Args);
                App.SetBusy(false);
            }

            // 自动检查更新
            var appConfigService = Services.GetRequiredService<AppConfigService>();
            if (appConfigService.UserConfig.AutoCheckUpdate)
            {
                var updateService = Services.GetRequiredService<UpdateService>();
                _ = updateService.CheckUpdate(false);
            }

            ShortcutHelper.EnsureQuickShareShortcutInSendTo(Path.Combine(AppContext.BaseDirectory, "QuickShare.exe"));
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
            _ = Services.GetRequiredService<MessageBoxService>().ShowMessage(
                "错误",
                $"出现了一个意外的错误：{e.Exception.Message}\n\n请查看日志文件以获取更多详细信息。");
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
