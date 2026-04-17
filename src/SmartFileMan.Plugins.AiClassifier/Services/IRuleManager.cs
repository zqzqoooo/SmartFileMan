using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// 规则管理器接口
/// Rule Manager Interface
/// </summary>
public interface IRuleManager
{
    /// <summary>
    /// 创建规则
    /// Create rule
    /// </summary>
    Task<Models.ClassificationRule> CreateRuleAsync(Models.ClassificationRule rule);

    /// <summary>
    /// 更新规则
    /// Update rule
    /// </summary>
    Task UpdateRuleAsync(Models.ClassificationRule rule);

    /// <summary>
    /// 删除规则
    /// Delete rule
    /// </summary>
    Task DeleteRuleAsync(string ruleId);

    /// <summary>
    /// 获取所有规则
    /// Get all rules
    /// </summary>
    List<Models.ClassificationRule> GetAllRules();

    /// <summary>
    /// 获取启用的规则
    /// Get enabled rules
    /// </summary>
    List<Models.ClassificationRule> GetEnabledRules();

    /// <summary>
    /// 匹配文件适用的规则
    /// Match applicable rules for a file
    /// </summary>
    List<Models.ClassificationRule> MatchRules(IFileEntry file);

    /// <summary>
    /// 启用/禁用规则
    /// Enable/disable rule
    /// </summary>
    Task SetEnabledAsync(string ruleId, bool enabled);
}
