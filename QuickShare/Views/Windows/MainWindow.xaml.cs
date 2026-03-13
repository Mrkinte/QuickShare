using QuickShare.Helpers;
using QuickShare.Services;
using QuickShare.ViewModels.Windows;
using QuickShare.Views.Pages;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private const int WM_COPYDATA = 0x004A;
        private readonly AppConfigService _appConfigService;
        private readonly IContentDialogService _contentDialogService;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public MainWindow(
            MainWindowViewModel viewModel,
            ISnackbarService snackbarService,
            AppConfigService appConfigService,
            INavigationService navigationService,
            IContentDialogService contentDialogService,
            INavigationViewPageProvider navigationViewPageProvider
        )
        {
            DataContext = viewModel;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            _appConfigService = appConfigService;
            _contentDialogService = contentDialogService;
            _contentDialogService.SetDialogHost(RootContentDialog);
            navigationService.SetNavigationControl(RootNavigation);
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e) => OnClosed(e);

        private async void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (_appConfigService.UserConfig.DisableCloseMessage)
            {
                if (_appConfigService.UserConfig.ExitDirectly)
                {
                    OnClosed(e);
                    return;
                }
                else
                {
                    Hide();
                    ShowInTaskbar = false;
                    return;
                }
            }

            var closeContentDialog = new CloseContentDialog(
                _contentDialogService.GetDialogHostEx(),
                _appConfigService);
            ContentDialogResult result = await closeContentDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (closeContentDialog.DisableCloseMessageCheckBox.IsChecked == true)
                {
                    _appConfigService.UserConfig.DisableCloseMessage = true;
                    _appConfigService.SaveConfig();
                }

                if (closeContentDialog.ExitRadioButton.IsChecked == true)
                {
                    if (closeContentDialog.ExitRadioButton.IsChecked != _appConfigService.UserConfig.ExitDirectly)
                    {
                        _appConfigService.UserConfig.ExitDirectly = true;
                        _appConfigService.SaveConfig();
                    }
                    OnClosed(e);
                    return;
                }
                else
                {
                    if (closeContentDialog.ExitRadioButton.IsChecked != _appConfigService.UserConfig.ExitDirectly)
                    {
                        _appConfigService.UserConfig.ExitDirectly = false;
                        _appConfigService.SaveConfig();
                    }
                    Hide();
                    ShowInTaskbar = false;
                    return;
                }
            }
        }

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
                    Dispatcher.Invoke(async () =>
                    {
                        await SendToHelper.ProcessSendToFilesAsync(files);
                        if (Visibility != Visibility.Visible)
                        {
                            Visibility = Visibility.Visible;
                        }
                        if (WindowState == WindowState.Minimized)
                        {
                            WindowState = WindowState.Normal;
                        }
                        Show();
                        ShowInTaskbar = true;
                        Topmost = true;
                        Activate();
                        Focus();
                        Topmost = false;
                    });
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
