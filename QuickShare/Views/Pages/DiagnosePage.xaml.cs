using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// DiagnosePage.xaml 的交互逻辑
    /// </summary>
    public partial class DiagnosePage : Page
    {
        public DiagnosePage(DiagnoseViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
