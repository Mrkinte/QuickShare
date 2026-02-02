using QuickShare.ViewModels.Pages;

namespace QuickShare.Views.Pages
{
    public partial class SharePage
    {
        public SharePage(ShareViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
