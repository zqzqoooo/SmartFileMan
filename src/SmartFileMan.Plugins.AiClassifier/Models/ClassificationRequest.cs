namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// LLM 分类请求
/// LLM Classification Request
/// </summary>
public class ClassificationRequest
{
    /// <summary>
    /// 用户定义的分类规则（自然语言）
    /// User-defined classification rule (natural language)
    /// </summary>
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 待分类文件元数据列表
    /// List of file metadata to classify
    /// </summary>
    public List<FileMetadata> Files { get; set; } = new();
}
