using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartFileMan.Contracts.Models
{
    // 文件的核心抽象。
    // 在系统中，所有文件都是这个对象，而不是字符串路径。
    public interface IFileEntry
    {
        // 文件的唯一 ID (可以使用 Hash 或 路径的 Base64，用于数据库索引)
        string Id { get; }

        // 文件名 (例如: "report.pdf")
        string Name { get; }

        // 扩展名 (例如: ".pdf")，统一转为小写
        string Extension { get; }

        // 父文件夹路径
        string DirectoryPath { get; }

        // 完整路径 (注意：在 iOS/Android 某些特殊目录下可能无法直接访问)
        string FullPath { get; }

        // 文件大小 (字节)
        long SizeBytes { get; }

        // 创建时间
        DateTime CreationTime { get; }

        // 最后修改时间
        DateTime LastWriteTime { get; }

        // --- 核心能力方法 ---

        // 获取文件的哈希值 (计算通常比较慢，所以是异步的)
        Task<string> GetHashAsync();

        // 打开文件流 (用于读取内容，如 AI 分析、读取视频头信息)
        Task<Stream> OpenReadAsync();

        // 保留这个泛型接口，以后官方媒体插件可以用
        Task<T?> GetMetadataAsync<T>() where T : class;

        // 万能口袋：自定义属性字典
        // 插件可以往这里写数据，UI 可以读这里的数据
        IDictionary<string, object> Properties { get; }
    }
}