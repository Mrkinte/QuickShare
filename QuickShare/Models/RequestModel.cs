namespace QuickShare.Models
{
    public partial class RequestModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _agreeButtonText = string.Empty;

        [ObservableProperty]
        private string _refuseButtonText = string.Empty;

        [ObservableProperty]
        private Visibility _visibility = Visibility.Visible;

        public string Name { get; set; } = string.Empty;

        public string Uuid { get; set; } = string.Empty;

        public int Type { get; set; } = 0;  // 0:上传请求    1:文字信息

        public int Status { get; set; } = 0; // 0:待处理    1:同意    2:拒绝

        public int TotalSeconds { get; set; } = 0;

        public int RequestCounts { get; set; } = 0; // 剩余请求次数，清零后视为请求过期。
    }
}
