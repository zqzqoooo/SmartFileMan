namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 剧集信息（用于电视剧）
/// Episode information for TV shows
/// </summary>
public class EpisodeInfo
{
    /// <summary>所属季号</summary>
    public int SeasonNumber { get; set; }

    /// <summary>集号</summary>
    public int EpisodeNumber { get; set; }

    /// <summary>集标题</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>集简介</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>海报路径</summary>
    public string? StillPath { get; set; }

    /// <summary>上映日期</summary>
    public DateTime? AirDate { get; set; }
}
