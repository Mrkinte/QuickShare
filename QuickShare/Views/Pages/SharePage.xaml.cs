using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// SharePage.xaml 的交互逻辑
    /// </summary>
    public partial class SharePage : Page
    {
        public SharePage(ShareViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
