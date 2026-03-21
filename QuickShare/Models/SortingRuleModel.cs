using System.Collections.ObjectModel;

namespace QuickShare.Models
{
    public partial class SortingRuleModel : ObservableObject
    {
        public long Id { get; set; }

        [ObservableProperty]
        private string _sortingName = string.Empty;

        [ObservableProperty]
        private string _savePath = "\\";

        [ObservableProperty]
        private ObservableCollection<string> _extension = new();
    }
}
