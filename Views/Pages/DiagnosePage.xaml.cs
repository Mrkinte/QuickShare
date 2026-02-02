using QuickShare.ViewModels.Pages;

namespace QuickShare.Views.Pages
{
    public partial class DiagnosePage
    {
        public DiagnosePage(DiagnoseViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
