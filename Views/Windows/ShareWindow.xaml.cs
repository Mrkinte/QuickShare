using QuickShare.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Windows
{
    /// <summary>
    /// ShareWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShareWindow : FluentWindow
    {
        public ShareWindow(ShareWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
