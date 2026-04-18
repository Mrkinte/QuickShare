using QuickShare.Services;
using Serilog;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Clipboard = System.Windows.Clipboard;

namespace QuickShare.ViewModels.Pages
{
    public partial class TextMessageViewModel(
        ILogger logger,
        ISnackbarService snackbarService,
        MessageBoxService messageBoxService,
        INavigationService navigationService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _sender = string.Empty;

        [ObservableProperty]
        private string _message = string.Empty;

        [RelayCommand]
        private void OnCopyText()
        {
            Clipboard.SetText(Message);
            snackbarService.Show(
                "提示",
                $"复制成功：{Message}",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                TimeSpan.FromSeconds(5));
        }

        [RelayCommand]
        private async Task OnExportTxt()
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = $"{Sender}_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt"
            })
            {
                try
                {
                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    await File.WriteAllTextAsync(saveFileDialog.FileName, Message, Encoding.UTF8);

                    snackbarService.Show(
                        "提示",
                        $"成功导出文本消息。",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    logger.Error($"导出文本消息失败: {ex.Message}");
                    await messageBoxService.ShowMessage("错误", $"导出文本消息失败: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void OnExitTextMessage() => navigationService.GoBack();

        #region INavigationAware Members

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        #endregion
    }
}
