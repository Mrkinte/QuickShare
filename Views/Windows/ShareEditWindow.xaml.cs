using QuickShare.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Windows
{
    /// <summary>
    /// ShareEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShareEditWindow : FluentWindow
    {
        public ShareEditWindow(ShareEditWindowViewModel viewModel)
        {
            Owner = App.Current.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
