namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// LLM 分类响应
/// LLM Classification Response
/// </summary>
public class ClassificationResponse
{
    /// <summary>
    /// 分类操作列表
    /// List of classification operations
    /// </summary>
    public List<ClassificationOperation> Operations { get; set; } = new();

    /// <summary>
    /// 处理状态
    /// Processing status: success, no_match, error
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    /// AI 理解的规则摘要
    /// AI's summary of the understood rule
    /// </summary>
    public string RuleSummary { get; set; } = string.Empty;
}

/// <summary>
/// 单个分类操作
/// Single classification operation
/// </summary>
public class ClassificationOperation
{
    /// <summary>
    /// 源文件相对路径
    /// Source file relative path
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 目标目录相对路径
    /// Target directory relative path
    /// </summary>
    public string TargetDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型（MOVE 或 RENAME）
    /// Operation type (MOVE or RENAME)
    /// </summary>
    public string Operation { get; set; } = "MOVE";

    /// <summary>
    /// 置信度 0-100
    /// Confidence score 0-100
    /// </summary>
    public int Confidence { get; set; } = 50;
}
