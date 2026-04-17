using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Contracts.Storage;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 缓存服务实现
/// Cache service implementation using LiteDB
/// </summary>
public class CacheService : ICacheService
{
    private readonly IPluginStorage _storage;
    private const string CacheCollectionPrefix = "tmdb_cache_";
    private const string FileIndexCollectionPrefix = "file_index_";
    private const string ConfigKey = "plugin_config";
    private const string AllIndicesKey = "all_file_indices_keys";

    public CacheService(IPluginStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc />
    public MediaInfo? GetCached(string cacheKey)
    {
        var entry = _storage.Load<TmdbCacheEntry>($"{CacheCollectionPrefix}{cacheKey}");
        if (entry == null) return null;

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return entry.Data;
    }

    /// <inheritdoc />
    public void SaveToCache(string cacheKey, MediaInfo mediaInfo, int ttl = 168)
    {
        var entry = new TmdbCacheEntry
        {
            CacheKey = cacheKey,
            TmdbId = mediaInfo.TmdbId,
            MediaType = mediaInfo.MediaType,
            Data = mediaInfo,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(ttl)
        };

        _storage.Save($"{CacheCollectionPrefix}{cacheKey}", entry);
    }

    /// <inheritdoc />
    public MediaFileIndex? GetFileIndex(string path)
    {
        var allKeys = _storage.Load<List<string>>(AllIndicesKey) ?? new List<string>();
        foreach (var key in allKeys)
        {
            var item = _storage.Load<MediaFileIndex>($"{FileIndexCollectionPrefix}{key}");
            if (item != null && string.Equals(item.OriginalPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }

    /// <inheritdoc />
    public void SaveFileIndex(MediaFileIndex index)
    {
        index.UpdatedAt = DateTime.UtcNow;
        _storage.Save($"{FileIndexCollectionPrefix}{index.Id}", index);

        // Keep track of all keys
        var allKeys = _storage.Load<List<string>>(AllIndicesKey) ?? new List<string>();
        if (!allKeys.Contains(index.Id))
        {
            allKeys.Add(index.Id);
            _storage.Save(AllIndicesKey, allKeys);
        }
    }

    /// <inheritdoc />
    public List<MediaFileIndex> GetAllFileIndices()
    {
        var allKeys = _storage.Load<List<string>>(AllIndicesKey) ?? new List<string>();
        var result = new List<MediaFileIndex>();
        foreach (var key in allKeys)
        {
            var item = _storage.Load<MediaFileIndex>($"{FileIndexCollectionPrefix}{key}");
            if (item != null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public void ClearExpiredCache()
    {
    }

    /// <inheritdoc />
    public string GenerateCacheKey(int tmdbId, string mediaType)
    {
        return $"tmdb_{tmdbId}_{mediaType}";
    }

    /// <inheritdoc />
    public void SaveConfig(PluginConfig config)
    {
        _storage.Save(ConfigKey, config);
    }

    /// <inheritdoc />
    public PluginConfig GetConfig()
    {
        var config = _storage.Load<PluginConfig>(ConfigKey);
        return config ?? new PluginConfig();
    }
}
