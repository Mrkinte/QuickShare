using QuickShare.ViewModels.Pages;
using System.Windows;

namespace QuickShare.Views.Pages
{
    public partial class SettingsPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void RestoreConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            RestoreConfigPopup.IsOpen = !RestoreConfigPopup.IsOpen;
        }
    }
}
