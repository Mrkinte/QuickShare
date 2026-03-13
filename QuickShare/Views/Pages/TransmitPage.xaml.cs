using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// TransmitPage.xaml 的交互逻辑
    /// </summary>
    public partial class TransmitPage : Page
    {
        public TransmitPage(TransmitViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
