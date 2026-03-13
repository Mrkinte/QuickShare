using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class ShareEditViewModel(
        ILogger logger,
        SqliteService sqliteService,
        ISnackbarService snackbarService,
        MessageBoxService messageBoxService,
        INavigationService navigationService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private const int MaxFileCountThreshold = 10000;
        public event EventHandler<long>? ShareRecordModified;

        [ObservableProperty]
        private ShareRecordModel _shareRecord = new();

        [ObservableProperty]
        private ObservableCollection<FileRecordModel> _fileRecords = new();

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _verificationCode = string.Empty;

        [ObservableProperty]
        private ControlAppearance _modifyBtnAppearance = ControlAppearance.Secondary;

        [ObservableProperty]
        private bool _modifyBtnEnabled = false;

        [ObservableProperty]
        private ObservableCollection<FileRecordModel> _folders = new();

        [RelayCommand]
        private async Task OnAddFilesToShareRecord()
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
                        List<string> waitAddFiles = new();      // 待添加的文件列表。
                        foreach (var filePath in fileDialog.FileNames)
                        {
                            if (FileRecords.Any(f => f.FileName == Path.GetFileName(filePath)))
                            {
                                continue;
                            }
                            waitAddFiles.Add(filePath);
                        }
                        if (fileDialog.FileNames.Length != waitAddFiles.Count)
                        {
                            snackbarService.Show(
                                "提示",
                                "选择的文件中，部分文件可能已在分享列表中。",
                                ControlAppearance.Info,
                                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                                TimeSpan.FromSeconds(5));
                        }
                        if (waitAddFiles.Count == 0)
                        {
                            return;
                        }
                        var shareId = await Task.Run(() => sqliteService.AddFileRecords(ShareRecord.ShareId, waitAddFiles.ToArray()));
                        FileRecords = new ObservableCollection<FileRecordModel>(sqliteService.ReadFileRecordsByShareId(ShareRecord.ShareId));
                        ShareRecordModified?.Invoke(this, ShareRecord.ShareId);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _ = messageBoxService.ShowMessage("错误", "添加文件失败，部分文件无访问权限。");
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
                finally
                {
                    App.SetBusy(false);
                }
            }
        }

        [RelayCommand]
        private async Task OnAddDirectoryToShareRecord()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                try
                {
                    App.SetBusy(true);
                    DialogResult result = folderDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        if (FileRecords.Any(f => f.FileName == Path.GetFileName(folderDialog.SelectedPath)))
                        {
                            snackbarService.Show(
                                "提示",
                                "选择的文件夹已在分享列表中。",
                                ControlAppearance.Info,
                                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                                TimeSpan.FromSeconds(5));
                            return;
                        }
                        var fileCount = await Task.Run(() => CustomHelper.GetFileCount(folderDialog.SelectedPath));
                        if (fileCount > MaxFileCountThreshold)
                        {
                            _ = messageBoxService.ShowMessage("警告", $"选择的文件夹中包含超过{MaxFileCountThreshold}个文件，建议归档或压缩后再分享。");
                            return;
                        }
                        var shareId = await Task.Run(() => sqliteService.AddFileRecords(ShareRecord.ShareId, [folderDialog.SelectedPath]));
                        FileRecords = new ObservableCollection<FileRecordModel>(sqliteService.ReadFileRecordsByShareId(ShareRecord.ShareId));
                        ShareRecordModified?.Invoke(this, ShareRecord.ShareId);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _ = messageBoxService.ShowMessage("错误", "添加文件夹失败，部分文件或文件夹无访问权限。");
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
        private void OnRandomVerificationCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = Random.Shared;
            VerificationCode = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            ModifyBtnAppearance = ControlAppearance.Caution;
            ModifyBtnEnabled = true;
        }

        [RelayCommand]
        private void OnConfirmModify()
        {
            if (Regex.IsMatch(VerificationCode, @"[^a-zA-Z0-9]"))
            {
                snackbarService.Show(
                    "警告",
                    "验证码中存在非法字符，仅支字母和数字。",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5));
                return;
            }

            sqliteService.UpdateSingleValue("ShareRecords", "ShareId", ShareRecord.ShareId, "Description", Description);
            sqliteService.UpdateSingleValue("ShareRecords", "ShareId", ShareRecord.ShareId, "VerificationCode", VerificationCode);

            ModifyBtnAppearance = ControlAppearance.Secondary;
            ModifyBtnEnabled = false;
            ShareRecordModified?.Invoke(this, ShareRecord.ShareId);
        }

        [RelayCommand]
        private void OnConfirmModifyEnable()
        {
            ModifyBtnAppearance = ControlAppearance.Caution;
            ModifyBtnEnabled = true;
        }

        [RelayCommand]
        private void OnExitEdit() => navigationService.GoBack();

        [RelayCommand]
        private void OnFolderSelected(object item)
        {
            if (item is not FileRecordModel selectedFolder)
            {
                return;
            }
            var index = Folders.IndexOf(selectedFolder);
            List<FileRecordModel> ps;
            if (index == 0)
            {
                ps = sqliteService.ReadFileRecordsByShareId(Folders[index].FileId);
            }
            else
            {
                ps = sqliteService.ReadFileRecordsByFileId(Folders[index].FileId);
            }
            if (ps.Count == 0)
            {
                return;
            }
            FileRecords = new ObservableCollection<FileRecordModel>(ps);
            for (int i = Folders.Count - 1; i > index; i--)
            {
                Folders.RemoveAt(i);
            }
        }

        [RelayCommand]
        private void OnOpenSubDirectory(FileRecordModel fileRecord)
        {
            if (!fileRecord.IsDirectory)
            {
                return;
            }
            var ps = sqliteService.ReadFileRecordsByFileId(fileRecord.FileId);
            FileRecords = new ObservableCollection<FileRecordModel>(ps);
            Folders.Add(fileRecord);
        }

        [RelayCommand]
        private void OnCopyFilePath(FileRecordModel fileRecord)
        {
        }

        [RelayCommand]
        private void OnShowInExplorer(FileRecordModel fileRecord)
        {
        }

        [RelayCommand]
        private async Task OnDelete(FileRecordModel fileRecord)
        {
            if (FileRecords.Count == 1)
            {
                snackbarService.Show(
                    "警告",
                    "至少需要存在一个分享文件。",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5));
                return;
            }
            App.SetBusy(true);
            await Task.Run(() => sqliteService.DeleteFileRecord(fileRecord.FileId));
            FileRecords.Remove(fileRecord);
            App.SetBusy(false);
        }

        public void UpdateShareRecord(long shareId)
        {
            ShareRecord = sqliteService.ReadShareRecord(shareId);
            if (ShareRecord.FileRecords != null)
            {
                Description = ShareRecord.Description;
                VerificationCode = ShareRecord.VerificationCode;
                FileRecords = new ObservableCollection<FileRecordModel>(ShareRecord.FileRecords);
            }
            Folders.Clear();
            var rootFileRecord = new FileRecordModel
            {
                FileId = shareId,
                ParentDirectoryId = 0,
                FileName = "根目录",
                DownloadCount = 0
            };
            Folders.Add(rootFileRecord);
        }

        #region INavigationAware Members

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            ModifyBtnAppearance = ControlAppearance.Secondary;
            ModifyBtnEnabled = false;
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
