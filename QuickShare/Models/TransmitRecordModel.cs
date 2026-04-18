namespace QuickShare.Models
{
    public partial class TransmitRecordModel : ObservableObject
    {
        public string Time { get; set; } = string.Empty;

        public int Type { get; set; } = 0;  // 0:上传请求    1:文字信息

        public int Direction { get; set; } = 0; // 0:上传（接收）    1:下载（发送）

        public string Message { get; set; } = string.Empty;

        public List<TransmitFileDetail> Files { get; set; } = new List<TransmitFileDetail>();

        public int Status { get; set; } = 0; // 0:待处理    1:同意    2:拒绝
    }

    public class TransmitFileDetail
    {
        public string Name { get; set; } = string.Empty;

        public long Size { get; set; } = 0;
    }
}
