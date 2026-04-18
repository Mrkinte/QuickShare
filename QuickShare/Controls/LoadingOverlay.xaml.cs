using System.Windows.Controls;

namespace QuickShare.Controls
{
    /// <summary>
    /// LoadingOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingOverlay : UserControl
    {
        public LoadingOverlay()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
        }

        #region 依赖属性

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                "IsBusy",
                typeof(bool),
                typeof(LoadingOverlay),
                new PropertyMetadata(false, OnIsBusyChanged));

        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var loadingOverlay = (LoadingOverlay)d;
            var isVisible = (bool)e.NewValue;

            loadingOverlay.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            if (isVisible)
            {
                loadingOverlay.Focus();
            }
        }

        #endregion
    }
}
