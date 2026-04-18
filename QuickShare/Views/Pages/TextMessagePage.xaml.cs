using QuickShare.ViewModels.Pages;
using System.Windows.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// SharePage.xaml 的交互逻辑
    /// </summary>
    public partial class TextMessagePage : Page
    {
        public TextMessagePage(TextMessageViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
