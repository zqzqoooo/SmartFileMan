using LiteDB;
using SmartFileMan.Contracts.Services;
using System;
using System.IO;
using System.Threading;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 文件追踪实现类 (基于 LiteDB)
    /// File Tracker Implementation (Based on LiteDB)
    /// </summary>
    public class FileTracker : IFileTracker
    {
        private readonly LiteDatabase _db;
        private const string CollectionName = "file_tracker";

        public FileTracker(LiteDatabase db)
        {
            _db = db;
            EnsureIndexesWithRetry();
        }

        private void EnsureIndexesWithRetry()
        {
            // 确保索引存在以提高查询速度
            // Ensure index exists to improve query speed
            const int maxAttempts = 5;
            const int delayMs = 200;
            var col = _db.GetCollection<FileRecord>(CollectionName);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    col.EnsureIndex(x => x.Id);
                    // 为新路径建立索引，以便检查已移动的文件
                    // Index NewPath for checking moved files
                    col.EnsureIndex(x => x.NewPath);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(delayMs);
                }
            }

            throw new InvalidOperationException(
                "Failed to initialize LiteDB indexes due to file lock.\n" +
                "LiteDB 索引初始化失败，数据库文件被其它进程占用。" +
                "\nPlease ensure no other SmartFileMan instance or external tool holds the database file.");
        }

        public bool IsProcessed(string filePath, DateTime lastWriteTime, long sizeBytes)
        {
            var col = _db.GetCollection<FileRecord>(CollectionName);
            
            // Check if filePath matches Original ID
            var record = col.FindById(filePath);

            // If not found by ID, check if it matches NewPath
            if (record == null)
            {
                record = col.FindOne(x => x.NewPath == filePath);
            }

            if (record == null) return false;

            // Check if file has been modified (Time or Size changed)
            if (Math.Abs((record.LastWriteTime - lastWriteTime).TotalSeconds) > 2 || record.SizeBytes != sizeBytes)
            {
                return false;
            }

            return true;
        }

        public void TrackProcessed(string originalPath, string hash, long size, DateTime lastWriteTime, 
                                   string pluginId, string newPath, string status)
        {
            var col = _db.GetCollection<FileRecord>(CollectionName);
            var record = new FileRecord
            {
                Id = originalPath,
                OriginalPath = originalPath,
                FileHash = hash,
                SizeBytes = size,
                LastWriteTime = lastWriteTime,
                ResponsiblePluginId = pluginId,
                NewPath = newPath,
                Status = status,
                ProcessedAt = DateTime.Now,
                NewFilename = !string.IsNullOrEmpty(newPath) ? System.IO.Path.GetFileName(newPath) : ""
            };
            col.Upsert(record);
        }

        public void Forget(string filePath)
        {
            var col = _db.GetCollection<FileRecord>(CollectionName);
            col.Delete(filePath);
        }

        /// <summary>
        /// 更新文件的哈希值（用于延迟计算哈希的场景）
        /// Update file hash value (for scenarios where hash is computed with delay)
        /// </summary>
        public void UpdateHash(string originalPath, string hash)
        {
            var col = _db.GetCollection<FileRecord>(CollectionName);
            var record = col.FindById(originalPath);

            if (record != null)
            {
                record.FileHash = hash;
                col.Update(record);
            }
        }

        private class FileRecord
        {
            [BsonId]
            public string Id { get; set; } = string.Empty; // FilePath as ID
            public string OriginalPath { get; set; } = string.Empty;
            public string FileHash { get; set; }
            public long SizeBytes { get; set; }
            public DateTime LastWriteTime { get; set; }
            
            public string ResponsiblePluginId { get; set; }
            public string NewPath { get; set; }
            public string NewFilename { get; set; }
            public string Status { get; set; } = string.Empty;
            
            public DateTime ProcessedAt { get; set; }
        }
    }
}
