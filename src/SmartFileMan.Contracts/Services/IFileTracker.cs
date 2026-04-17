using System;
using System.Threading.Tasks;

namespace SmartFileMan.Contracts.Services
{
    /// <summary>
    /// 文件追踪服务：记录文件的处理状态，避免重复处理
    /// File Tracker Service: Records file processing status to avoid duplicate processing
    /// </summary>
    public interface IFileTracker
    {
        /// <summary>
        /// 检查文件是否已被处理过且未发生变更
        /// Check if the file has been processed and has not changed
        /// </summary>
        bool IsProcessed(string filePath, DateTime lastWriteTime, long sizeBytes);

        /// <summary>
        /// 追踪文件处理状态（支持更详细的信息，如文件哈希、插件ID等）
        /// Track file processing status (supports more detailed information, such as file hash, plugin ID, etc.)
        /// </summary>
        void TrackProcessed(string originalPath, string hash, long size, DateTime lastWriteTime, 
                            string pluginId, string newPath, string status);

        /// <summary>
        /// 清除文件的追踪记录 (例如文件处理失败需要重试)
        /// Clear file tracking record (e.g., file processing failed and needs retry)
        /// </summary>
        void Forget(string filePath);

        /// <summary>
        /// 更新文件的哈希值（用于延迟计算哈希的场景）
        /// Update file hash value (for scenarios where hash is computed with delay)
        /// </summary>
        void UpdateHash(string originalPath, string hash);
    }
}
