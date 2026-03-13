using System.Text.Json.Serialization;

namespace QuickShare.Models
{
    public class ShareRecordModel
    {
        [JsonIgnore]
        public long ShareId { get; set; }

        public string Description { get; set; } = string.Empty;

        public string CreateTime { get; set; } = "00-00-00 00:00:00";

        public long FileCount { get; set; }

        public long DirectoryCount { get; set; }

        [JsonIgnore]
        public string VerificationCode { get; set; } = string.Empty;

        public List<FileRecordModel>? FileRecords { get; set; }
    }

    public class FileRecordModel
    {
        public long FileId { get; set; }

        public long ParentDirectoryId { get; set; }

        public long DownloadCount { get; set; }

        public string FileName { get; set; } = string.Empty;

        public long FileCount { get; set; }

        public long DirectoryCount { get; set; }

        public long Size { get; set; }

        public bool IsDirectory { get; set; }
    }

    /// <summary>
    /// 发送给前端的文件属性模型。
    /// </summary>
    public class FileProps
    {
        public string Name { get; set; } = string.Empty;
        public long FileId { get; set; }
        public string Extension { get; set; } = string.Empty;
    }

    public class ShareProps
    {
        public string Description { get; set; } = string.Empty;
        public string CreateTime { get; set; } = "00-00-00 00:00:00";
        public long FileCount { get; set; }
        public long DirectoryCount { get; set; }
    }
}
