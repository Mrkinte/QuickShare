using Microsoft.Data.Sqlite;
using QuickShare.Models;
using Serilog;
using System.Data;
using System.IO;

namespace QuickShare.Services
{
    public class SqliteService
    {
        private SqliteConnection _connection;
        private readonly ILogger _logger;

        public SqliteService(ILogger logger)
        {
            try
            {
                _logger = logger;
                var dbPath = Path.Combine(AppContext.BaseDirectory, "sqlite.db");
                _connection = new SqliteConnection($"Data Source={dbPath}");
                _connection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open SQLite database.", ex);
            }

            using var createMainTableCmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS ShareRecords (
                    ShareId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Description TEXT,
                    CreateTime TEXT NOT NULL,
                    FileCount INTEGER NOT NULL DEFAULT 0,
                    DirectoryCount INTEGER NOT NULL DEFAULT 0,
                    VerificationCode TEXT
                )", _connection);
            createMainTableCmd.ExecuteNonQuery();

            using var createFileRecordsTableCmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS FileRecords (
                    ShareId INTEGER,
                    FileId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ParentDirectoryId INTEGER,   -- NULL=根路径
                    DownloadCount INTEGER NOT NULL DEFAULT 0,
                    FileName TEXT NOT NULL,
                    IsDirectory INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(ShareId) REFERENCES ShareRecords(ShareId) ON DELETE CASCADE
                )", _connection);
            createFileRecordsTableCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 关闭Sqlite数据库连接。
        /// </summary>
        public void Close()
        {
            _connection.CloseAsync();
            _connection.DisposeAsync();
        }

        /// <summary>
        /// 添加分享记录，返回生成的ShareId
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public long AddShareRecord(string[] paths)
        {
            long fileCount = 0;
            long directoryCount = 0;
            using var transaction = _connection.BeginTransaction();
            try
            {
                // ShareRecord
                using var insertShareCmd = new SqliteCommand(@"
                    INSERT INTO ShareRecords (CreateTime) VALUES (@CreateTime);
                    SELECT last_insert_rowid();", _connection, transaction);
                insertShareCmd.Parameters.AddWithValue("@CreateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                object? insertShareCmdResutlt = insertShareCmd.ExecuteScalar();
                if (insertShareCmdResutlt == null)
                {
                    throw new Exception("Insert share record failed, no shareId returned.");
                }

                // FileRecords
                long shareId = (long)insertShareCmdResutlt;
                using var insertFileRecordCmd = new SqliteCommand(@"
                    INSERT INTO FileRecords (ShareId, FileName, IsDirectory) 
                    VALUES (@ShareId, @FileName, @IsDirectory);
                    SELECT last_insert_rowid();", _connection, transaction);
                foreach (string path in paths)
                {
                    insertFileRecordCmd.Parameters.Clear();
                    insertFileRecordCmd.Parameters.AddWithValue("@ShareId", shareId);
                    insertFileRecordCmd.Parameters.AddWithValue("@FileName", path);     // 根目录记录存储完整路径，子目录记录存储相对路径，减少存储空间占用。
                    insertFileRecordCmd.Parameters.AddWithValue("@IsDirectory", Directory.Exists(path) ? 1 : 0);
                    object? insertFileRecordCmdResutlt = insertFileRecordCmd.ExecuteScalar();
                    if (insertFileRecordCmdResutlt == null)
                    {
                        throw new Exception("Insert path record failed, no pathId returned.");
                    }
                    long pathId = (long)insertFileRecordCmdResutlt;
                    if (Directory.Exists(path))
                    {
                        long subFileCount = 0;
                        long subDirectoryCount = 0;
                        (subFileCount, subDirectoryCount) = AddSubDirectoryRecords(shareId, pathId, path, transaction);
                        fileCount += subFileCount;
                        directoryCount += subDirectoryCount;
                    }
                    else if (File.Exists(path))
                    {
                        fileCount++;
                    }
                }
                transaction.Commit();
                UpdateFileAndDirectoryCountToShareRecords(shareId, fileCount, directoryCount);
                return shareId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 删除分享记录。
        /// </summary>
        /// <param name="shareId"></param>
        /// <returns></returns>
        public int DeleteShareRecord(long shareId)
        {
            using var deleteCmd = new SqliteCommand(
                "DELETE FROM ShareRecords WHERE ShareId = @ShareId", _connection);


            using var deleteCmd2 = new SqliteCommand(@"
                WITH DeletedFileRecords AS (
                    SELECT ShareId FROM FileRecords WHERE ShareId = @ShareId
                )
                DELETE FROM FileRecords WHERE id IN(SELECT id FROM DeletedFileRecords);", _connection);
            deleteCmd.Parameters.AddWithValue("@ShareId", shareId);
            return deleteCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除文件记录。
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public int DeleteFileRecord(long fileId)
        {
            var fileRecords = ReadFileRecord(fileId);

            using var deleteCmd = new SqliteCommand(
                "DELETE FROM FileRecords WHERE FileId = @FileId", _connection);
            deleteCmd.Parameters.AddWithValue("@FileId", fileId);
            return deleteCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 添加路径记录，成功则返回True。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="paths"></param>
        public bool AddFileRecords(long shareId, string[] paths)
        {
            long fileCount = 0;
            long directoryCount = 0;
            using var transaction = _connection.BeginTransaction();
            try
            {
                using var insertFileRecordCmd = new SqliteCommand(@"
                    INSERT INTO FileRecords (ShareId, FileName, IsDirectory) 
                    VALUES (@ShareId, @FileName, @IsDirectory);
                    SELECT last_insert_rowid();", _connection, transaction);
                foreach (string path in paths)
                {
                    insertFileRecordCmd.Parameters.Clear();
                    insertFileRecordCmd.Parameters.AddWithValue("@ShareId", shareId);
                    insertFileRecordCmd.Parameters.AddWithValue("@FileName", path);
                    insertFileRecordCmd.Parameters.AddWithValue("@IsDirectory", Directory.Exists(path) ? 1 : 0);
                    object? insertFileRecordCmdResutlt = insertFileRecordCmd.ExecuteScalar();
                    if (insertFileRecordCmdResutlt == null)
                    {
                        throw new Exception("Insert path record failed, no pathId returned.");
                    }
                    long pathId = (long)insertFileRecordCmdResutlt;
                    if (Directory.Exists(path))
                    {
                        long subFileCount = 0;
                        long subDirectoryCount = 0;
                        (subFileCount, subDirectoryCount) = AddSubDirectoryRecords(shareId, pathId, path, transaction);
                        fileCount += subFileCount;
                        directoryCount += subDirectoryCount;
                    }
                    else if (File.Exists(path))
                    {
                        fileCount++;
                    }
                }
                transaction.Commit();
                UpdateFileAndDirectoryCountToShareRecords(shareId, fileCount, directoryCount);
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 根据分享Id从数据库中读取分享记录。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="includeFileRecords"></param>
        /// <returns></returns>
        public ShareRecordModel ReadShareRecord(long shareId, bool includeFileRecords = true)
        {
            using var readShareRecordCmd = new SqliteCommand(@"
                SELECT ShareId, Description, CreateTime, FileCount, DirectoryCount, VerificationCode FROM ShareRecords
                WHERE ShareId = @ShareId", _connection);
            readShareRecordCmd.Parameters.AddWithValue("@ShareId", shareId);
            using var reader = readShareRecordCmd.ExecuteReader();
            var record = new ShareRecordModel();
            while (reader.Read())
            {
                record.ShareId = reader.GetInt64("ShareId");
                record.Description = reader["Description"]?.ToString() ?? string.Empty;
                record.CreateTime = reader.GetString("CreateTime");
                record.FileCount = reader.GetInt64("FileCount");
                record.DirectoryCount = reader.GetInt64("DirectoryCount");
                record.VerificationCode = reader["VerificationCode"]?.ToString() ?? string.Empty;
            }
            if (includeFileRecords)
            {
                record.FileRecords = ReadFileRecordsByShareId(shareId);
            }
            return record;
        }

        /// <summary>
        /// 读取所有分享记录。
        /// </summary>
        /// <returns></returns>
        public List<ShareRecordModel> ReadAllShareRecords()
        {
            var shareRecords = new List<ShareRecordModel>();
            using var readAllShareCmd = new SqliteCommand(
                "SELECT ShareId, Description, CreateTime, FileCount, DirectoryCount FROM ShareRecords", _connection);
            using var reader = readAllShareCmd.ExecuteReader();
            while (reader.Read())
            {
                var record = new ShareRecordModel();
                record.ShareId = reader.GetInt64("ShareId");
                record.Description = reader["Description"]?.ToString() ?? string.Empty;
                record.CreateTime = reader.GetString("CreateTime");
                record.FileCount = reader.GetInt64("FileCount");
                record.DirectoryCount = reader.GetInt64("DirectoryCount");
                shareRecords.Add(record);
            }

            return shareRecords;
        }

        /// <summary>
        /// 通过ShareId获取旗下的FileRecords。
        /// </summary>
        /// <param name="shareId"></param>
        /// <returns></returns>
        public List<FileRecordModel> ReadFileRecordsByShareId(long shareId)
        {
            var fileRecords = new List<FileRecordModel>();
            using var readCmd = new SqliteCommand(@"
                SELECT FileId, ParentDirectoryId, DownloadCount, FileName, IsDirectory FROM FileRecords
                WHERE ShareId = @ShareId AND ParentDirectoryId IS NULL", _connection);
            readCmd.Parameters.AddWithValue("@ShareId", shareId);
            using var reader = readCmd.ExecuteReader();
            while (reader.Read())
            {
                var record = new FileRecordModel();
                record.FileId = reader.GetInt64("FileId");
                object parentDirectoryIdObj = reader["ParentDirectoryId"];
                record.ParentDirectoryId = (parentDirectoryIdObj == DBNull.Value) ? 0 : (long)parentDirectoryIdObj;
                record.DownloadCount = reader.GetInt64("DownloadCount");
                record.FileName = Path.GetFileName(reader.GetString("FileName"));
                record.IsDirectory = reader.GetInt32("IsDirectory") == 1;
                fileRecords.Add(record);
            }
            return fileRecords;
        }

        /// <summary>
        /// 通过FileId获取旗下的FileRecords。
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public List<FileRecordModel> ReadFileRecordsByFileId(long fileId)
        {
            var fileRecords = new List<FileRecordModel>();
            using var readCmd = new SqliteCommand(@"
                SELECT FileId, ParentDirectoryId, DownloadCount, FileName, IsDirectory FROM FileRecords
                WHERE ParentDirectoryId = @ParentDirectoryId", _connection);
            readCmd.Parameters.AddWithValue("@ParentDirectoryId", fileId);
            using var reader = readCmd.ExecuteReader();
            while (reader.Read())
            {
                var record = new FileRecordModel();
                record.FileId = reader.GetInt64("FileId");
                object parentDirectoryIdObj = reader["ParentDirectoryId"];
                record.ParentDirectoryId = (parentDirectoryIdObj == DBNull.Value) ? 0 : (long)parentDirectoryIdObj;
                record.DownloadCount = reader.GetInt64("DownloadCount");
                record.FileName = reader.GetString("FileName");
                record.IsDirectory = reader.GetInt32("IsDirectory") == 1;
                fileRecords.Add(record);
            }
            return fileRecords;
        }

        /// <summary>
        /// 通过FileId获取对应的FileRecordModel对象。
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public FileRecordModel ReadFileRecord(long fileId)
        {
            using var readCmd = new SqliteCommand(@"
                SELECT FileId, ParentDirectoryId, DownloadCount, FileName, IsDirectory FROM FileRecords
                WHERE FileId = @FileId", _connection);
            readCmd.Parameters.AddWithValue("@FileId", fileId);
            using var reader = readCmd.ExecuteReader();
            var record = new FileRecordModel();
            while (reader.Read())
            {
                record.FileId = reader.GetInt64("FileId");
                object parentDirectoryIdObj = reader["ParentDirectoryId"];
                record.ParentDirectoryId = (parentDirectoryIdObj == DBNull.Value) ? 0 : (long)parentDirectoryIdObj;
                record.DownloadCount = reader.GetInt64("DownloadCount");
                record.FileName = reader.GetString("FileName");
                record.IsDirectory = reader.GetInt32("IsDirectory") == 1;
            }
            return record;
        }

        /// <summary>
        /// 获取完整路径。通过递归查询父路径直到根路径，拼接出完整路径。
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public string GetFullPath(long fileId)
        {
            List<string> pathParts = new List<string>();
            using var readShareCmd = new SqliteCommand(@"
                SELECT ParentDirectoryId, FileName FROM FileRecords
                WHERE FileId = @FileId", _connection);

            long parentFileId = fileId;

            while (parentFileId > 0)
            {
                readShareCmd.Parameters.Clear();
                readShareCmd.Parameters.AddWithValue("@FileId", parentFileId);
                using var reader = readShareCmd.ExecuteReader();

                while (reader.Read())
                {
                    object parentDirectoryIdObj = reader["ParentDirectoryId"];
                    parentFileId = (parentDirectoryIdObj == DBNull.Value) ? 0 : (long)parentDirectoryIdObj;

                    pathParts.Add(reader.GetString("FileName"));
                }
            }

            pathParts.Reverse();
            return string.Join("/", pathParts).Replace("\\", "/");
        }

        /// <summary>
        /// 增加下载次数，每当有文件被下载时，调用此方法增加下载次数。
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public int IncrementDownloadCount(long fileId)
        {
            using var updateCmd = new SqliteCommand(@"
                UPDATE FileRecords
                SET DownloadCount = DownloadCount + 1
                WHERE FileId = @FileId", _connection);
            updateCmd.Parameters.AddWithValue("FileId", fileId);
            return updateCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 更新单个字段的值。
        /// </summary>
        /// <param name="table"></param>
        /// <param name="searchParam"></param>
        /// <param name="searchValue"></param>
        /// <param name="updateParam"></param>
        /// <param name="updateValue"></param>
        /// <returns></returns>
        public int UpdateSingleValue(
            string table,
            string searchParam,
            object searchValue,
            string updateParam,
            object updateValue)
        {
            using var updateCmd = new SqliteCommand(@$"
                UPDATE {table}
                SET {updateParam} = @{updateParam}
                WHERE {searchParam} = @{searchParam}", _connection);
            updateCmd.Parameters.AddWithValue(updateParam, updateValue);
            updateCmd.Parameters.AddWithValue(searchParam, searchValue);
            return updateCmd.ExecuteNonQuery();
        }


        #region Private Methods

        /// <summary>
        /// 遍历子目录并添加记录。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="parentDirectoryId"></param>
        /// <param name="path"></param>
        /// <param name="transaction"></param>
        /// <exception cref="Exception"></exception>
        private (long, long) AddSubDirectoryRecords(long shareId, long parentDirectoryId, string path, SqliteTransaction transaction)
        {
            long fileCount = 0;
            long directoryCount = 0;
            using var insertFileRecordCmd = new SqliteCommand(@"
                    INSERT INTO FileRecords (ShareId, ParentDirectoryId, DownloadCount, FileName, IsDirectory) 
                    VALUES (@ShareId, @ParentDirectoryId, 0, @FileName, @IsDirectory);
                    SELECT last_insert_rowid();", _connection, transaction);
            try
            {
                var dirs = Directory.GetDirectories(path);
                directoryCount += dirs.Length;
                foreach (var dir in dirs)
                {
                    insertFileRecordCmd.Parameters.Clear();
                    insertFileRecordCmd.Parameters.AddWithValue("@ShareId", shareId);
                    insertFileRecordCmd.Parameters.AddWithValue("@ParentDirectoryId", parentDirectoryId);
                    insertFileRecordCmd.Parameters.AddWithValue("@FileName", Path.GetRelativePath(path, dir));   // 存储相对路径，减少存储空间占用。
                    insertFileRecordCmd.Parameters.AddWithValue("@IsDirectory", 1);
                    object? insertFileRecordCmdResutlt = insertFileRecordCmd.ExecuteScalar();
                    if (insertFileRecordCmdResutlt == null)
                    {
                        throw new Exception("Insert path record failed, no pathId returned.");
                    }
                    long insertFileId = (long)insertFileRecordCmdResutlt;
                    if (Directory.Exists(dir))
                    {
                        long subFileCount = 0;
                        long subDirectoryCount = 0;
                        (subFileCount, subDirectoryCount) = AddSubDirectoryRecords(shareId, insertFileId, dir, transaction);
                        fileCount += subFileCount;
                        directoryCount += subDirectoryCount;
                    }
                }

                var files = Directory.GetFiles(path);
                fileCount += files.Length;
                foreach (var file in files)
                {
                    insertFileRecordCmd.Parameters.Clear();
                    insertFileRecordCmd.Parameters.AddWithValue("@ShareId", shareId);
                    insertFileRecordCmd.Parameters.AddWithValue("@ParentDirectoryId", parentDirectoryId);
                    insertFileRecordCmd.Parameters.AddWithValue("@FileName", Path.GetRelativePath(path, file));   // 存储相对路径，减少存储空间占用。
                    insertFileRecordCmd.Parameters.AddWithValue("@IsDirectory", 0);
                    object? insertFileRecordCmdResutlt = insertFileRecordCmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            return (fileCount, directoryCount);
        }

        /// <summary>
        /// 更新ShareRecords表中的FileCount和DirectoryCount字段的值。当添加或删除文件记录时，调用此方法更新对应的ShareRecords记录中的文件和目录数量统计。
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="fileCount"></param>
        /// <param name="directoryCount"></param>
        private void UpdateFileAndDirectoryCountToShareRecords(long shareId, long fileCount, long directoryCount)
        {
            using var updateCmd = new SqliteCommand(@"
                UPDATE ShareRecords
                SET FileCount = FileCount + @FileCount,
                    DirectoryCount = DirectoryCount + @DirectoryCount
                WHERE ShareId = @ShareId", _connection);
            updateCmd.Parameters.AddWithValue("@FileCount", fileCount);
            updateCmd.Parameters.AddWithValue("@DirectoryCount", directoryCount);
            updateCmd.Parameters.AddWithValue("@ShareId", shareId);
            updateCmd.ExecuteNonQuery();
        }

        #endregion
    }
}
