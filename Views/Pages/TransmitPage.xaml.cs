using QuickShare.ViewModels.Pages;

namespace QuickShare.Views.Pages
{
    public partial class TransmitPage
    {
        public TransmitPage(TransmitViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
