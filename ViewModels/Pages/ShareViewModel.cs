using QuickShare.Models;
using QuickShare.Services;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Wpf.Ui.Abstractions.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class ShareViewModel(
            SqliteService sqliteService,
            ShareWindowManage shareWindowManage,
            ShareEditWindowManage shareEditWindowManage,
            TranslationService trService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<ShareModel>? _shareHistories;

        [RelayCommand]
        private void OnCreateShare()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = trService.Translate("Share_Text_FileSelectorTitle"),
                Multiselect = true,
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var id = sqliteService.AddShareHistory(openFileDialog.FileNames);
                var history = sqliteService.ReadShareHistory(id);
                OnOpenEditDialog(history);
            }
        }

        [RelayCommand]
        private void OnOpenEditDialog(ShareModel share)
        {
            shareEditWindowManage.ShareEditDone += (s, e) =>
            {
                OnRefreshHistories();
            };
            shareEditWindowManage.CreateShareEditWindow(share);
        }

        [RelayCommand]
        private void OnOpenShareDialog(object parameter)
        {
            ShareModel? shareHistory = parameter as ShareModel;
            if (shareHistory == null) return;
            shareWindowManage.CreateShareWindow(shareHistory);
        }

        [RelayCommand]
        private void OnDelete(object parameter)
        {
            ShareModel? shareHistory = parameter as ShareModel;
            if (shareHistory == null) return;
            sqliteService.DeleteShareHistory(shareHistory.Id);
            ShareHistories = new ObservableCollection<ShareModel>(
                sqliteService.ReadAllShareHistory());
        }

        [RelayCommand]
        private void OnRefreshHistories()
        {
            ShareHistories = new ObservableCollection<ShareModel>(
                sqliteService.ReadAllShareHistory());
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
            OnRefreshHistories();
            sqliteService.ShareDataChanged += (s, e) =>
            {
                OnRefreshHistories();
            };
            _isInitialized = true;
        }

        #endregion
    }
}
