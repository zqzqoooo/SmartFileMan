namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// 文件元数据（精简版，用于 LLM 请求）
/// File metadata (lightweight, for LLM request)
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// 文件名
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 相对路径
    /// Relative path
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 扩展名（含点）
    /// Extension (with dot)
    /// </summary>
    public string Extension { get; set; } = string.Empty;
}
