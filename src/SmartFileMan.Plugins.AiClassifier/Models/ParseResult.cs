namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// 解析结果
/// Parser result
/// </summary>
public class ParseResult
{
    /// <summary>
    /// 是否有效
    /// Whether the result is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 解析后的操作列表
    /// Parsed operations
    /// </summary>
    public List<ClassificationOperation> Operations { get; set; } = new();

    /// <summary>
    /// 错误列表
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// Warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 路径冲突
/// Path conflict
/// </summary>
public class PathConflict
{
    /// <summary>
    /// 文件1
    /// File 1
    /// </summary>
    public string File1 { get; set; } = string.Empty;

    /// <summary>
    /// 文件2
    /// File 2
    /// </summary>
    public string File2 { get; set; } = string.Empty;

    /// <summary>
    /// 冲突类型
    /// Conflict type: duplicate_target, nonexistent_source
    /// </summary>
    public string ConflictType { get; set; } = string.Empty;
}
