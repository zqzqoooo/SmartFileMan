namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 处理状态枚举
/// Processing status enumeration
/// </summary>
public enum ProcessingStatus
{
    /// <summary>待处理</summary>
    Pending = 0,

    /// <summary>已识别</summary>
    Recognized = 1,

    /// <summary>已匹配</summary>
    Matched = 2,

    /// <summary>已重命名</summary>
    Renamed = 3,

    /// <summary>处理失败</summary>
    Failed = 4
}

/// <summary>
/// 文件索引记录
/// File index record for tracked media files
/// </summary>
public class MediaFileIndex
{
    /// <summary>唯一标识符</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>原始文件路径</summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>原始文件名</summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>当前文件路径（重命名后）</summary>
    public string? CurrentPath { get; set; }

    /// <summary>媒体类型</summary>
    public string MediaType { get; set; } = "movie";

    /// <summary>关联的TMDB ID</summary>
    public int? TmdbId { get; set; }

    /// <summary>季号（电视剧）</summary>
    public int? SeasonNumber { get; set; }

    /// <summary>集号（电视剧）</summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>文件哈希（用于去重）</summary>
    public string? FileHash { get; set; }

    /// <summary>处理状态</summary>
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
