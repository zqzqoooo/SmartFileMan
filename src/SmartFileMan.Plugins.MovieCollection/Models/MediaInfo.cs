namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 影视媒体信息
/// Media information for movies and TV shows
/// </summary>
public class MediaInfo
{
    /// <summary>TMDB ID</summary>
    public int TmdbId { get; set; }

    /// <summary>媒体类型：movie 或 tv</summary>
    public string MediaType { get; set; } = "movie";

    /// <summary>标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>原标题</summary>
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>简介</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>海报路径（本地缓存）</summary>
    public string? PosterPath { get; set; }

    /// <summary>背景图路径</summary>
    public string? BackdropPath { get; set; }

    /// <summary>评分（0-10）</summary>
    public double VoteAverage { get; set; }

    /// <summary>投票人数</summary>
    public int VoteCount { get; set; }

    /// <summary>上映/首播日期</summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>电影片长（分钟）</summary>
    public int? Runtime { get; set; }

    /// <summary>电视剧总季数</summary>
    public int? NumberOfSeasons { get; set; }

    /// <summary>电视剧总集数</summary>
    public int? NumberOfEpisodes { get; set; }

    /// <summary>类型列表</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>创作国家</summary>
    public List<string> ProductionCountries { get; set; } = new();

    /// <summary>导演列表</summary>
    public List<string> Directors { get; set; } = new();

    /// <summary>演员列表（前10位）</summary>
    public List<CastMember> Cast { get; set; } = new();

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
