using QuickShare.Models;
using QuickShare.ViewModels.Windows;
using QuickShare.Views.Windows;
using Wpf.Ui;

namespace QuickShare.Services
{
    public class ShareWindowManage(
        AppConfigService appConfigService,
        ISnackbarService snackbarService,
        ShareEditWindowManage shareEditWindowManage,
        TranslationService translationService)
    {
        public void CreateShareWindow(ShareModel share)
        {
            var viewModel = new ShareWindowViewModel(
                share,
                appConfigService,
                snackbarService,
                shareEditWindowManage,
                translationService);
            var shareWindow = new ShareWindow(viewModel);
            shareWindow.Show();
        }
    }

    public class ShareEditWindowManage(
        SqliteService sqliteService,
        ISnackbarService snackbarService,
        TranslationService translationService)
    {
        public event EventHandler? ShareEditDone;

        public void CreateShareEditWindow(ShareModel share)
        {
            var viewModel = new ShareEditWindowViewModel(share, sqliteService,
                snackbarService, translationService);

            // Notify SharePage to refresh
            EventHandler? shareEditDoneHandler = null;
            shareEditDoneHandler = (s, e) =>
            {
                ShareEditDone?.Invoke(this, EventArgs.Empty);
                viewModel.ShareEditDone -= shareEditDoneHandler;
                shareEditDoneHandler = null;
            };
            viewModel.ShareEditDone += shareEditDoneHandler;

            var shareEditWindow = new ShareEditWindow(viewModel);
            shareEditWindow.ShowDialog();
        }
    }
}
