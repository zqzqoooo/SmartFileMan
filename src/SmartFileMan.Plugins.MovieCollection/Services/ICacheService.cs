using SmartFileMan.Plugins.MovieCollection.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 缓存服务接口
/// Cache service interface for TMDB data persistence
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存的媒体信息
    /// Get cached media information
    /// </summary>
    MediaInfo? GetCached(string cacheKey);

    /// <summary>
    /// 保存媒体信息到缓存
    /// Save media information to cache
    /// </summary>
    void SaveToCache(string cacheKey, MediaInfo mediaInfo, int ttl = 168);

    /// <summary>
    /// 获取文件索引
    /// Get file index by path
    /// </summary>
    MediaFileIndex? GetFileIndex(string path);

    /// <summary>
    /// 保存或更新文件索引
    /// Save or update file index
    /// </summary>
    void SaveFileIndex(MediaFileIndex index);

    /// <summary>
    /// 获取所有文件索引
    /// Get all file indices
    /// </summary>
    List<MediaFileIndex> GetAllFileIndices();

    /// <summary>
    /// 清除过期缓存
    /// Clear expired cache entries
    /// </summary>
    void ClearExpiredCache();

    /// <summary>
    /// 生成缓存键
    /// Generate cache key
    /// </summary>
    string GenerateCacheKey(int tmdbId, string mediaType);

    /// <summary>
    /// 保存配置
    /// Save plugin configuration
    /// </summary>
    void SaveConfig(PluginConfig config);

    /// <summary>
    /// 获取配置
    /// Get plugin configuration
    /// </summary>
    PluginConfig GetConfig();
}
