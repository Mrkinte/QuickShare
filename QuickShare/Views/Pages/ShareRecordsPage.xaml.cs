using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// ShareRecordsPage.xaml 的交互逻辑
    /// </summary>
    public partial class ShareRecordsPage : Page
    {
        public ShareRecordsPage(ShareRecordsViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
