using System.IO;
using System.Text.Json.Serialization;

namespace QuickShare.Models
{
    public class ShareModel
    {
        [JsonIgnore]
        public long Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public string CreateTime { get; set; } = "00-00-00 00:00:00";

        [JsonIgnore]
        public string VerifyCode { get; set; } = string.Empty;

        public long TotalSize
        {
            get
            {
                if (ShareFiles == null) return 0;
                return ShareFiles.Sum(f => f != null ? f.Size : 0);
            }
        }

        public long FileCount
        {
            get
            {
                if (ShareFiles == null) return 0;
                return ShareFiles.Count();
            }
        }

        public List<ShareFileModel>? ShareFiles { get; set; }
    }

    public class ShareFileModel
    {
        public long Id { get; set; }

        public string Name
        {
            get
            {
                return System.IO.Path.GetFileName(Path);
            }
        }

        public long Size
        {
            get
            {
                var fileInfo = new FileInfo(Path);
                if (!fileInfo.Exists) return 0;
                return fileInfo.Length;
            }
        }

        public long DownloadCount { get; set; }

        public bool IsValid
        {
            get
            {
                return File.Exists(Path);
            }
        }

        [JsonIgnore]
        public string Path { get; set; } = string.Empty;
    }
}
