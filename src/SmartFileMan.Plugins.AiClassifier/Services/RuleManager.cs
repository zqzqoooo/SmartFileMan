using SmartFileMan.Contracts.Storage;
using SmartFileMan.Contracts.Models;
using System.Collections.Generic;

namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// 规则管理器实现
/// Rule Manager Implementation
/// </summary>
public class RuleManager : IRuleManager
{
    private readonly IPluginStorage _storage;
    private const string RulesCollectionKey = "ClassificationRules_Ids";

    public RuleManager(IPluginStorage storage)
    {
        _storage = storage;
    }

    public Task<Models.ClassificationRule> CreateRuleAsync(Models.ClassificationRule rule)
    {
        rule.Id = Guid.NewGuid().ToString();
        rule.CreatedAt = DateTime.UtcNow;
        _storage.Save($"{RulesCollectionKey}{rule.Id}", rule);

        var ids = GetRuleIds();
        ids.Add(rule.Id);
        _storage.Save(RulesCollectionKey, ids);

        return Task.FromResult(rule);
    }

    public Task UpdateRuleAsync(Models.ClassificationRule rule)
    {
        _storage.Save($"{RulesCollectionKey}{rule.Id}", rule);
        return Task.CompletedTask;
    }

    public Task DeleteRuleAsync(string ruleId)
    {
        _storage.Save($"{RulesCollectionKey}{ruleId}", (Models.ClassificationRule?)null);

        var ids = GetRuleIds();
        ids.Remove(ruleId);
        _storage.Save(RulesCollectionKey, ids);

        return Task.CompletedTask;
    }

    public List<Models.ClassificationRule> GetAllRules()
    {
        var rules = new List<Models.ClassificationRule>();
        var ids = GetRuleIds();

        foreach (var id in ids)
        {
            var rule = _storage.Load<Models.ClassificationRule>($"{RulesCollectionKey}{id}");
            if (rule != null)
                rules.Add(rule);
        }

        return rules.OrderByDescending(r => r.Priority).ToList();
    }

    public List<Models.ClassificationRule> GetEnabledRules()
    {
        return GetAllRules().Where(r => r.IsEnabled).ToList();
    }

    public List<Models.ClassificationRule> MatchRules(IFileEntry file)
    {
        var enabledRules = GetEnabledRules();
        var matched = new List<Models.ClassificationRule>();

        foreach (var rule in enabledRules)
        {
            if (MatchesCondition(file, rule.Condition))
            {
                matched.Add(rule);
            }
        }

        return matched.OrderByDescending(r => r.Priority).ToList();
    }

    public Task SetEnabledAsync(string ruleId, bool enabled)
    {
        var rule = _storage.Load<Models.ClassificationRule>($"{RulesCollectionKey}{ruleId}");
        if (rule != null)
        {
            rule.IsEnabled = enabled;
            _storage.Save($"{RulesCollectionKey}{ruleId}", rule);
        }
        return Task.CompletedTask;
    }

    private List<string> GetRuleIds()
    {
        return _storage.Load<List<string>>(RulesCollectionKey) ?? new List<string>();
    }

    private bool MatchesCondition(IFileEntry file, Models.RuleCondition? condition)
    {
        if (condition == null)
            return true;

        if (condition.Extensions?.Count > 0)
        {
            if (!condition.Extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (condition.Keywords?.Count > 0)
        {
            if (!condition.Keywords.Any(k => file.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}
