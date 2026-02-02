using Microsoft.Data.Sqlite;
using QuickShare.Models;
using System.Data;
using System.IO;

namespace QuickShare.Services
{
    public class SqliteService
    {
        private SqliteConnection _connection;

        public event EventHandler? ShareDataChanged;

        public SqliteService()
        {
            try
            {
                _connection = new SqliteConnection("Data Source=sqlite.db");
                _connection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open SQLite database.", ex);
            }

            using var createMainTableCmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS ShareHistory (
                    ShareId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Description TEXT,
                    CreateTime TEXT NOT NULL,
                    VerifyCode TEXT
                )", _connection);
            createMainTableCmd.ExecuteNonQuery();

            using var createPathTableCmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS FilePath (
                    ShareId INTEGER NOT NULL REFERENCES ShareHistory(ShareId),
                    FileId INTEGER PRIMARY KEY AUTOINCREMENT,
                    DownloadCount INTEGER NOT NULL,
                    Path TEXT NOT NULL,
                    FOREIGN KEY(ShareId) REFERENCES ShareHistory(ShareId) ON DELETE CASCADE
                )", _connection);
            createPathTableCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Close the database connection
        /// </summary>
        public void Close()
        {
            _connection.CloseAsync();
            _connection.DisposeAsync();
        }

        /// <summary>
        /// Add share history to database
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public long AddShareHistory(string[] paths)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                // Insert into ShareHistory table
                using var insertShareCmd = new SqliteCommand(@"
                    INSERT INTO ShareHistory (CreateTime) VALUES (@CreateTime);
                    SELECT last_insert_rowid();", _connection, transaction);
                insertShareCmd.Parameters.AddWithValue("@CreateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                object? resutlt = insertShareCmd.ExecuteScalar();
                if (resutlt == null)
                {
                    throw new Exception("Insert share history failed, no shareId returned.");
                }

                // Insert into FilePath table
                long shareId = (long)resutlt;
                using var insertPathCmd = new SqliteCommand(
                    "INSERT INTO FilePath (ShareId, DownloadCount, Path) VALUES (@ShareId, @DownloadCount, @Path);",
                    _connection, transaction);
                foreach (var p in paths)
                {
                    if (!File.Exists(p)) continue;

                    try
                    {
                        insertPathCmd.Parameters.Clear();
                        insertPathCmd.Parameters.AddWithValue("@ShareId", shareId);
                        insertPathCmd.Parameters.AddWithValue("@DownloadCount", 0);
                        insertPathCmd.Parameters.AddWithValue("@Path", p);
                        insertPathCmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to insert file path.", ex);
                    }
                }

                transaction.Commit();
                ShareDataChanged?.Invoke(this, EventArgs.Empty);
                return shareId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Failed to insert share history.", ex);
            }
        }

        /// <summary>
        /// delete share history from database
        /// </summary>
        /// <param name="shareId"></param>
        /// <returns>The number of rows inserted, updated, or deleted. -1 for SELECT statements.</returns>
        public int DeleteShareHistory(long shareId)
        {
            using var deleteShareCmd = new SqliteCommand(
                "DELETE FROM ShareHistory WHERE ShareId = @ShareId", _connection);
            deleteShareCmd.Parameters.AddWithValue("@ShareId", shareId);
            ShareDataChanged?.Invoke(this, EventArgs.Empty);
            return deleteShareCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// read share history by shareId from database
        /// </summary>
        /// <param name="shareId"></param>
        /// <returns></returns>
        public ShareModel ReadShareHistory(long shareId)
        {
            ShareModel history = new ShareModel();
            using var readShareCmd = new SqliteCommand(@"
                SELECT ShareId, Description, CreateTime, VerifyCode FROM ShareHistory
                WHERE ShareId = @ShareId", _connection);
            readShareCmd.Parameters.AddWithValue("@ShareId", shareId);
            using var reader = readShareCmd.ExecuteReader();
            while (reader.Read())
            {
                history.Id = reader.GetInt64("ShareId");
                history.Description = reader["Description"]?.ToString() ?? string.Empty;
                history.CreateTime = reader.GetString("CreateTime");
                history.VerifyCode = reader["VerifyCode"]?.ToString() ?? string.Empty;
                history.ShareFiles = ReadFileInfos(history.Id);
            }

            return history;
        }

        /// <summary>
        /// read all share history from database
        /// </summary>
        public List<ShareModel> ReadAllShareHistory()
        {
            var shareHistories = new List<ShareModel>();
            using var readAllShareCmd = new SqliteCommand(
                "SELECT ShareId, Description, CreateTime, VerifyCode FROM ShareHistory", _connection);

            using var reader = readAllShareCmd.ExecuteReader();
            while (reader.Read())
            {
                var history = new ShareModel();
                history.Id = reader.GetInt64("ShareId");
                history.Description = reader["Description"]?.ToString() ?? string.Empty;
                history.CreateTime = reader.GetString("CreateTime");
                history.VerifyCode = reader["VerifyCode"]?.ToString() ?? string.Empty;
                history.ShareFiles = ReadFileInfos(history.Id);
                shareHistories.Add(history);
            }

            return shareHistories;
        }

        /// <summary>
        /// read file infos by shareId from database
        /// </summary>
        /// <param name="shareId"></param>
        /// <returns></returns>
        public List<ShareFileModel> ReadFileInfos(long shareId)
        {
            var fileInfos = new List<ShareFileModel>();
            using var readShareCmd = new SqliteCommand(@"
                SELECT FileId, DownloadCount, Path FROM FilePath
                WHERE ShareId = @ShareId", _connection);
            readShareCmd.Parameters.AddWithValue("@ShareId", shareId);
            using var reader = readShareCmd.ExecuteReader();
            while (reader.Read())
            {
                var fileInfo = new ShareFileModel();
                fileInfo.Id = reader.GetInt64("FileId");
                fileInfo.DownloadCount = reader.GetInt64("DownloadCount");
                fileInfo.Path = reader.GetString("Path");
                fileInfos.Add(fileInfo);
            }

            return fileInfos;
        }

        /// <summary>
        /// read file path by fileId from database
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public string ReadFilePath(long fileId)
        {
            string filePath = string.Empty;
            using var readFilePathCmd = new SqliteCommand(
                "SELECT Path FROM FilePath WHERE FileId = @FileId", _connection);
            readFilePathCmd.Parameters.AddWithValue("@FileId", fileId);
            using var reader = readFilePathCmd.ExecuteReader();
            while (reader.Read())
            {
                filePath = reader.GetString("Path");
            }

            return filePath;
        }

        /// <summary>
        /// add share file to database
        /// </summary>
        /// <param name="shareId"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int AddShareFiles(long shareId, string[] paths)
        {
            using var insertPathCmd = new SqliteCommand(
                "INSERT INTO FilePath (ShareId, DownloadCount, Path) VALUES (@ShareId, @DownloadCount, @Path);",
                _connection);
            foreach (var path in paths)
            {
                insertPathCmd.Parameters.Clear();
                insertPathCmd.Parameters.AddWithValue("@ShareId", shareId);
                insertPathCmd.Parameters.AddWithValue("@DownloadCount", 0);
                insertPathCmd.Parameters.AddWithValue("@Path", path);
                try
                {
                    insertPathCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to insert file path.", ex);
                }
            }
            return paths.Length;
        }

        /// <summary>
        /// delete share file by fileId from database
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public int DeleteShareFile(long fileId)
        {
            using var deleteFileCmd = new SqliteCommand(
                "DELETE FROM FilePath WHERE FileId = @FileId", _connection);
            deleteFileCmd.Parameters.AddWithValue("@FileId", fileId);
            return deleteFileCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// download counts plus one by fileId
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public int IncrementDownloadCount(long fileId)
        {
            using var updateCmd = new SqliteCommand(@"
                UPDATE FilePath
                SET DownloadCount = DownloadCount + 1
                WHERE FileId = @FileId", _connection);
            updateCmd.Parameters.AddWithValue("FileId", fileId);
            return updateCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// update single value in database
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
    }
}