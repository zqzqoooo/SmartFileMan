namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 插件状态管理器
/// Plugin state management
/// </summary>
public class PluginState
{
    /// <summary>当前选中的媒体信息</summary>
    public MediaInfo? CurrentMedia { get; set; }

    /// <summary>当前批次的文件索引</summary>
    public List<MediaFileIndex> BatchFiles { get; set; } = new();

    /// <summary>搜索结果列表</summary>
    public List<MediaInfo> SearchResults { get; set; } = new();

    /// <summary>电视剧季信息</summary>
    public List<SeasonInfo> Seasons { get; set; } = new();

    /// <summary>加载状态</summary>
    public bool IsLoading { get; set; }

    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>插件配置</summary>
    public PluginConfig Config { get; set; } = new();
}
