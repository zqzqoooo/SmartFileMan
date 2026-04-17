namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// TMDB缓存记录
/// TMDB cache record
/// </summary>
public class TmdbCacheEntry
{
    /// <summary>缓存键</summary>
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>TMDB ID</summary>
    public int TmdbId { get; set; }

    /// <summary>媒体类型</summary>
    public string MediaType { get; set; } = "movie";

    /// <summary>缓存数据</summary>
    public MediaInfo? Data { get; set; }

    /// <summary>缓存时间</summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>过期时间</summary>
    public DateTime ExpiresAt { get; set; }
}
