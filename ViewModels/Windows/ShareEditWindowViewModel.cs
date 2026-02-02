using QuickShare.Models;
using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace QuickShare.ViewModels.Windows
{
    public partial class ShareEditWindowViewModel(
        ShareModel share,
        SqliteService sqliteService,
        ISnackbarService snackbarService,
        TranslationService trService) : ObservableObject
    {
        private List<ShareFileModel> _waitRemoveFiles = new();
        private List<string> _waitAddFiles = new();
        public event EventHandler? ShareEditDone;

        [ObservableProperty]
        private bool _isModified = false;

        [ObservableProperty]
        private ControlAppearance _modifyBtnAppearance = ControlAppearance.Primary;

        [ObservableProperty]
        private ShareModel _share = share;

        [ObservableProperty]
        private string _description = share.Description ?? string.Empty;

        [ObservableProperty]
        private string _verifyCode = share.VerifyCode;

        [ObservableProperty]
        private ObservableCollection<ShareFileModel> _shareFiles = new(share.ShareFiles!);

        [RelayCommand]
        private void OnAddFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = trService.Translate("ShareEdit_Text_FileSelectorTitle"),
                Multiselect = true,
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {

                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (ShareFiles.Any(f => f.Path == fileName)) continue;

                    _waitAddFiles.Add(fileName);
                    var newFile = new ShareFileModel()
                    {
                        Path = fileName,
                        DownloadCount = 0,
                    };
                    ShareFiles.Add(newFile);
                }

                IsModified = true;
                ModifyBtnAppearance = ControlAppearance.Caution;
            }
        }

        [RelayCommand]
        private void OnRandomAccessCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = Random.Shared;

            VerifyCode = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [RelayCommand]
        private void OnConfirmModifyEnable()
        {
            if (IsModified) return;
            IsModified = true;
            ModifyBtnAppearance = ControlAppearance.Caution;
        }

        [RelayCommand]
        private void OnConfirmModify()
        {
            if (Regex.IsMatch(VerifyCode, @"[^a-zA-Z0-9]"))
            {
                snackbarService.Show(
                trService.Translate("Tip"),
                trService.Translate("ShareEdit_Message_ModifyFailed1"),
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
                return;
            }

            if (ShareFiles.Count == 0)
            {
                snackbarService.Show(
                trService.Translate("Tip"),
                trService.Translate("ShareEdit_Message_ModifyFailed2"),
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
                return;
            }

            if (_waitRemoveFiles.Count > 0)
            {
                foreach (var file in _waitRemoveFiles)
                {
                    sqliteService.DeleteShareFile(file.Id);
                }
                _waitRemoveFiles.Clear();
            }

            if (_waitAddFiles.Count > 0)
            {
                sqliteService.AddShareFiles(Share.Id, _waitAddFiles.ToArray());
                _waitAddFiles.Clear();
            }

            // Update Description
            if (Description != Share.Description)
            {
                sqliteService.UpdateSingleValue(
                    "ShareHistory",
                    "ShareId",
                    Share.Id,
                    "Description",
                    Description);
            }

            // Update VerifyCode
            if (VerifyCode != Share.VerifyCode)
            {
                sqliteService.UpdateSingleValue(
                    "ShareHistory",
                    "ShareId",
                    Share.Id,
                    "VerifyCode",
                    VerifyCode);
            }


            ShareEditDone?.Invoke(this, EventArgs.Empty);
            IsModified = false;

            snackbarService.Show(
                trService.Translate("Tip"),
                trService.Translate("ShareEdit_Message_ModifyFailed3"),
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Tag24),
                TimeSpan.FromSeconds(3));
        }

        [RelayCommand]
        private void OnDelete(ShareFileModel file)
        {
            IsModified = true;
            ModifyBtnAppearance = ControlAppearance.Caution;
            if (Share.ShareFiles == null || file == null) return;
            ShareFiles.Remove(file);
            _waitRemoveFiles.Add(file);
        }
    }
}
