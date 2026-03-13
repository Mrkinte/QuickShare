namespace QuickShare.Services
{
    public class MessageBoxService
    {
        public async Task ShowMessage(string title, string message)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                CloseButtonText = "确认",
            };

            _ = await uiMessageBox.ShowDialogAsync();
        }
    }
}
