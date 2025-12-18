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

            // 初始化字典
            Properties = new Dictionary<string, object>();
        }

        // 基础属性实现
        public string Id => _fileInfo.FullName;
        public string Name => _fileInfo.Name;
        public string Extension => _fileInfo.Extension.ToLowerInvariant();
        public string DirectoryPath => _fileInfo.DirectoryName ?? string.Empty;
        public string FullPath => _fileInfo.FullName;
        public long SizeBytes => _fileInfo.Length;
        public DateTime CreationTime => _fileInfo.CreationTime;
        public DateTime LastWriteTime => _fileInfo.LastWriteTime;

        // 1. 实现万能属性字典
        public IDictionary<string, object> Properties { get; }

        public async Task<string> GetHashAsync()
        {
            if (!string.IsNullOrEmpty(_cachedHash)) return _cachedHash;

            // 之前的问题：计算了哈希但没有存进 _cachedHash 变量里
            // 修复如下：
            _cachedHash = await Task.Run(() =>
            {
                using var stream = _fileInfo.OpenRead();
                using var sha256 = SHA256.Create();
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            });

            return _cachedHash;
        }

        public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(_fileInfo.OpenRead());

        // 2. 实现泛型元数据接口 (签名必须完全一致，包括 where T : class)
        public Task<T?> GetMetadataAsync<T>() where T : class
        {
            // 暂时返回 null，表示没有特定元数据
            return Task.FromResult<T?>(null);
        }
    }
}