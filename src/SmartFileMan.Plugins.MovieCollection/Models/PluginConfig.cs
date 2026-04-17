namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 插件配置
/// Plugin configuration settings
/// </summary>
public class PluginConfig
{
    /// <summary>TMDB API Key</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>默认重命名模板</summary>
    public string RenameTemplate { get; set; } = "{title} S{season:00}E{episode:00}";

    /// <summary>是否自动处理</summary>
    public bool AutoProcess { get; set; } = false;

    /// <summary>目标文件夹（为空则原地重命名）</summary>
    public string? TargetFolder { get; set; }

    /// <summary>语言偏好</summary>
    public string Language { get; set; } = "zh-CN";
}
