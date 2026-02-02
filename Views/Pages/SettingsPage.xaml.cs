using QuickShare.ViewModels.Pages;

namespace QuickShare.Views.Pages
{
    public partial class SettingsPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void RestoreConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            RestoreConfigPopup.IsOpen = !RestoreConfigPopup.IsOpen;
        }
    }
}
