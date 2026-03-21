using Microsoft.Extensions.DependencyInjection;
using QuickShare.Services;
using QuickShare.ViewModels.Pages;
using QuickShare.Views.Pages;
using QuickShare.Views.Windows;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace QuickShare.Helpers
{
    public class SendToHelper
    {
        private const int MaxFileCountThreshold = 10000;

        public static async Task ProcessSendToFilesAsync(string[] filePaths)
        {
            var sqliteService = App.Services.GetRequiredService<SqliteService>();
            var shareViewModel = App.Services.GetRequiredService<ShareViewModel>();
            var messageBoxService = App.Services.GetRequiredService<MessageBoxService>();
            var mainWindow = App.Services.GetRequiredService<INavigationWindow>() as MainWindow;
            var snackbarService = App.Services.GetRequiredService<ISnackbarService>() as SnackbarService;
            if (filePaths.Length > MaxFileCountThreshold)
            {
                _ = messageBoxService.ShowMessage("提示", $"选择的文件数量超过{MaxFileCountThreshold}（包括文件夹中的文件），建议归档或压缩后再分享。");
                return;
            }
            long shareId = 0;
            foreach (var path in filePaths)
            {
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    // 如果路径既不是文件也不是文件夹，跳过并继续处理其他路径
                    return;
                }
                else if (Directory.Exists(path))
                {
                    var fileCount = await Task.Run(() => CustomHelper.GetFileCount(path));
                    if (fileCount > MaxFileCountThreshold)
                    {
                        _ = messageBoxService.ShowMessage("提示", $"选择的文件夹中包含超过{MaxFileCountThreshold}个文件，建议归档或压缩后再分享。");
                        return;
                    }
                }

                if (shareId == 0)
                {
                    shareId = sqliteService.AddShareRecord([path]);
                }
                else
                {
                    sqliteService.AddFileRecords(shareId, [path]);
                }
            }
            if (mainWindow != null && shareId != 0)
            {
                shareViewModel.UpdateShareRecord(shareId);
                mainWindow.Navigate(typeof(SharePage));

                if (snackbarService != null)
                {
                    snackbarService.Show(
                        "提示",
                        "已成功创建分享，请扫描二维码下载。",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
