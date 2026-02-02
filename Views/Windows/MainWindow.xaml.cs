using QuickShare.Services;
using QuickShare.ViewModels.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private bool _isRealExit = false;
        private bool _isFirstHidden = true;
        private readonly TranslationService _trService;
        private readonly SendToService _sendToService;
        private const int WM_COPYDATA = 0x004A;
        private TrayNotificationService _trayNotificationService;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService,
            INavigationService navigationService,
            TrayNotificationService trayNotificationService,
            TranslationService trService,
            SendToService sendToService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            _trService = trService;
            _sendToService = sendToService;

            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetDialogHost(RootContentDialog);
            navigationService.SetNavigationControl(RootNavigation);

            _trayNotificationService = trayNotificationService;
            _trayNotificationService.Initialize(TrayIcon);
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            _isRealExit = true;
            this.Close();
        }

        private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isRealExit)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;

                if (_isFirstHidden)
                {
                    _trayNotificationService.ShowNotification(
                        _trService.Translate("Tip"),
                        _trService.Translate("MainWindow_Message_Minimal"));
                    _isFirstHidden = false;
                }
                return;
            }
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_COPYDATA)
            {
                var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                if (cds.lpData != IntPtr.Zero)
                {
                    string data = Marshal.PtrToStringUni(cds.lpData, cds.cbData / 2);

                    var files = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    Dispatcher.Invoke(() => _sendToService.ProcessSendToFiles(files));
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
