using QuickShare.Services;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Pages
{
    /// <summary>
    /// CloseContentDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CloseContentDialog : ContentDialog
    {
        public CloseContentDialog(
            ContentDialogHost? dialogHost,
            AppConfigService appConfigService) : base(dialogHost)
        {
            InitializeComponent();
            if (appConfigService.UserConfig.ExitDirectly)
            {
                ExitRadioButton.IsChecked = true;
            }
            else
            {
                MinimizeRadioButton.IsChecked = true;
            }
            DisableCloseMessageCheckBox.IsChecked = appConfigService.UserConfig.DisableCloseMessage;
        }
    }
}
