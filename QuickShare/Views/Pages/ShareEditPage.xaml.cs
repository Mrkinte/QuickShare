using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// ShareEditPage.xaml 的交互逻辑
    /// </summary>
    public partial class ShareEditPage : Page
    {
        public ShareEditPage(ShareEditViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
