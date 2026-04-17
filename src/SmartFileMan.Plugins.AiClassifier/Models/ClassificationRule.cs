namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// 分类规则：定义自然语言分类逻辑
/// Classification Rule: Defines natural language classification logic
/// </summary>
public class ClassificationRule
{
    /// <summary>
    /// 规则唯一标识符
    /// Unique identifier for the rule
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 规则名称（用户友好）
    /// Rule name (user-friendly)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 自然语言描述的分类规则
    /// Natural language description of the classification rule
    /// </summary>
    /// <example>"将包含'发票'的PDF文件移动到 Documents/Invoices 目录"</example>
    public string NaturalLanguagePattern { get; set; } = string.Empty;

    /// <summary>
    /// 规则匹配条件（可选，用于预过滤）
    /// Rule matching conditions (optional, for pre-filtering)
    /// </summary>
    public RuleCondition? Condition { get; set; }

    /// <summary>
    /// 规则优先级（数值越大优先级越高）
    /// Rule priority (higher value = higher priority)
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// 是否启用
    /// Whether the rule is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后使用时间
    /// Last used timestamp
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 规则匹配条件
/// Rule matching conditions
/// </summary>
public class RuleCondition
{
    /// <summary>
    /// 文件扩展名列表（如 [".pdf", ".docx"]）
    /// List of file extensions (e.g., [".pdf", ".docx"])
    /// </summary>
    public List<string>? Extensions { get; set; }

    /// <summary>
    /// 文件名包含的关键词
    /// Keywords that the filename should contain
    /// </summary>
    public List<string>? Keywords { get; set; }

    /// <summary>
    /// 文件大小范围（字节）- 最小值
    /// File size range (bytes) - minimum
    /// </summary>
    public long? MinSize { get; set; }

    /// <summary>
    /// 文件大小范围（字节）- 最大值
    /// File size range (bytes) - maximum
    /// </summary>
    public long? MaxSize { get; set; }
}
