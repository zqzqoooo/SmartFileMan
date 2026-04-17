namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 电视剧季信息
/// Season information for TV shows
/// </summary>
public class SeasonInfo
{
    /// <summary>季号</summary>
    public int SeasonNumber { get; set; }

    /// <summary>季名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>季简介</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>海报路径</summary>
    public string? PosterPath { get; set; }

    /// <summary>集数</summary>
    public int EpisodeCount { get; set; }

    /// <summary>剧集列表</summary>
    public List<EpisodeInfo> Episodes { get; set; } = new();
}
