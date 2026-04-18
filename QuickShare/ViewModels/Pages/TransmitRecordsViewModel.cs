using QuickShare.Models;
using System.Collections.ObjectModel;
using Wpf.Ui.Abstractions.Controls;

namespace QuickShare.ViewModels.Pages
{
    public partial class TransmitRecordsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<TransmitRecordModel> _transmitRecords = new();

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
            TransmitRecords.Add(new TransmitRecordModel { Type = 0 });
            _isInitialized = true;
        }

        #endregion
    }
}
