using SmartFileMan.Plugins.MovieCollection.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// TMDB API 服务接口
/// TMDB API service interface
/// </summary>
public interface ITmdbService
{
    /// <summary>
    /// 搜索影视
    /// Search for movies or TV shows
    /// </summary>
    Task<List<MediaInfo>> SearchAsync(string query, string mediaType = "all");

    /// <summary>
    /// 获取电影详情
    /// Get movie details by ID
    /// </summary>
    Task<MediaInfo?> GetMovieDetailsAsync(int tmdbId);

    /// <summary>
    /// 获取电视剧详情
    /// Get TV show details by ID
    /// </summary>
    Task<MediaInfo?> GetTvDetailsAsync(int tmdbId);

    /// <summary>
    /// 获取电视剧季信息
    /// Get season details for a TV show
    /// </summary>
    Task<SeasonInfo?> GetSeasonDetailsAsync(int tmdbId, int seasonNumber);

    /// <summary>
    /// 下载并缓存图片
    /// Download and cache poster/backdrop image
    /// </summary>
    Task<string?> DownloadImageAsync(string path);
}
