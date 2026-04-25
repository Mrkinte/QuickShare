using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using QuickShare.Views.Pages;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class ShareRecordsViewModel(
        ILogger logger,
        SqliteService sqliteService,
        ShareViewModel shareViewModel,
        MessageBoxService messageBoxService,
        INavigationService navigationService,
        ShareEditViewModel shareEditViewModel) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private const int MaxFileCountThreshold = 10000;

        [ObservableProperty]
        private ObservableCollection<ShareRecordModel> _shareRecords = new ObservableCollection<ShareRecordModel>(sqliteService.ReadAllShareRecords());

        [RelayCommand]
        private async Task OnCreateShareFromFile()
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Title = "选择分享文件",
                Multiselect = true,
            })
            {
                try
                {
                    App.SetBusy(true);
                    DialogResult result = fileDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        if (fileDialog.FileNames.Length > MaxFileCountThreshold)
                        {
                            _ = messageBoxService.ShowMessage("提示", $"选择的文件数量超过{MaxFileCountThreshold}，建议归档或压缩后再分享。");
                            return;
                        }
                        var shareId = await Task.Run(() => sqliteService.AddShareRecord(fileDialog.FileNames));
                        ShareRecords.Add(sqliteService.ReadShareRecord(shareId));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _ = messageBoxService.ShowMessage("错误", "创建分享失败，部分文件无访问权限。");
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    _ = messageBoxService.ShowMessage("错误", ex.Message);
                }
                finally
                {
                    App.SetBusy(false);
                }
            }
        }

        [RelayCommand]
        private async Task OnCreateShareFromFolder()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                try
                {
                    App.SetBusy(true);
                    DialogResult result = folderDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var fileCount = await Task.Run(() => CustomHelper.GetFileCount(folderDialog.SelectedPath));
                        if (fileCount > MaxFileCountThreshold)
                        {
                            _ = messageBoxService.ShowMessage("提示", $"选择的文件夹中包含超过{MaxFileCountThreshold}个文件，建议归档或压缩后再分享。");
                            return;
                        }
                        var shareId = await Task.Run(() => sqliteService.AddShareRecord([folderDialog.SelectedPath]));
                        ShareRecords.Add(sqliteService.ReadShareRecord(shareId));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _ = messageBoxService.ShowMessage("错误", "创建分享失败，部分文件或文件夹无访问权限。");
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    _ = messageBoxService.ShowMessage("错误", ex.Message);
                }
                finally
                {
                    App.SetBusy(false);
                }
            }
        }

        [RelayCommand]
        private async Task OnClearShareRecordsAsync()
        {
            App.SetBusy(true);
            await Task.Run(() => sqliteService.ClearShareRecords());
            ShareRecords.Clear();
            App.SetBusy(false);
        }

        [RelayCommand]
        private void OnUpdateShareRecords()
        {
            ShareRecords = new ObservableCollection<ShareRecordModel>(sqliteService.ReadAllShareRecords());
        }

        [RelayCommand]
        private void OnEditShareRecord(ShareRecordModel shareRecord)
        {
            shareEditViewModel.UpdateShareRecord(shareRecord.ShareId);
            _ = navigationService.NavigateWithHierarchy(typeof(ShareEditPage));
        }

        [RelayCommand]
        private void OnOpenShareDialog(ShareRecordModel shareRecord)
        {
            shareViewModel.UpdateShareRecord(shareRecord.ShareId);
            _ = navigationService.NavigateWithHierarchy(typeof(SharePage));
        }

        [RelayCommand]
        private async Task OnDeleteShareRecord(ShareRecordModel shareRecord)
        {
            App.SetBusy(true);
            await Task.Run(() => sqliteService.DeleteShareRecord(shareRecord.ShareId));
            ShareRecords.Remove(shareRecord);
            App.SetBusy(false);
        }


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
            // 注册事件，当ShareRecord被修改时，更新修改内容到UI。
            shareEditViewModel.ShareRecordModified += (s, shareId) =>
            {
                var recordToReplace = ShareRecords.FirstOrDefault(f => f.ShareId == shareId);
                if (recordToReplace != null)
                {
                    int index = ShareRecords.IndexOf(recordToReplace);
                    ShareRecords[index] = sqliteService.ReadShareRecord(shareId);
                }
            };
            _isInitialized = true;
        }

        #endregion
    }
}
