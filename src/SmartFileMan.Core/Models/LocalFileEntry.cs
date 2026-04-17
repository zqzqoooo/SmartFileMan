using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Core.Models
{
    public class LocalFileEntry : IFileEntry
    {
        private readonly FileInfo _fileInfo;
        private string? _cachedHash;

        public LocalFileEntry(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            _fileInfo = new FileInfo(filePath);
        }

        // 基础属性实现
        // Basic Property Implementation
        public string Id => _fileInfo.FullName; // 使用全路径作为唯一标识 / Use full path as unique ID
        public string Name => _fileInfo.Name;
        public string Extension => _fileInfo.Extension.ToLowerInvariant();
        public string DirectoryPath => _fileInfo.DirectoryName ?? string.Empty;
        public string FullPath => _fileInfo.FullName;
        public long SizeBytes => _fileInfo.Length;
        public DateTime CreationTime => _fileInfo.CreationTime;
        public DateTime LastWriteTime => _fileInfo.LastWriteTime;

        // 扩展属性字典 (如 ID3 标签、图片尺寸)
        // Extended properties dictionary (e.g., ID3 tags, image dimensions)
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public Task<T?> GetMetadataAsync<T>() where T : class
        {
            // 以后可以接入 Windows Property System 或 Image/Video 库
            // Future integration with Windows Property System or Image/Video libraries
            return Task.FromResult<T?>(null);
        }

        public async Task<string> GetHashAsync()
        {
            // 缓存 Hash 避免重复计算
            // Cache Hash to avoid re-calculation
            if (_cachedHash != null) return _cachedHash;

            return await Task.Run(() =>
            {
                using var stream = _fileInfo.OpenRead();
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(stream);
                _cachedHash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                return _cachedHash;
            });
        }

        public Task<Stream> OpenReadAsync()
        {
            return Task.FromResult<Stream>(_fileInfo.OpenRead());
        }
    }
}