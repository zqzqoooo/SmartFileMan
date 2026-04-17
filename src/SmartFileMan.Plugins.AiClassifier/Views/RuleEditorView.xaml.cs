using SmartFileMan.Plugins.AiClassifier.Models;
using SmartFileMan.Plugins.AiClassifier.Services;

namespace SmartFileMan.Plugins.AiClassifier.Views;

public partial class RuleEditorView : ContentPage
{
    private readonly IRuleManager _ruleManager;
    private readonly ClassificationRule? _existingRule;
    private readonly Action _onSaved;

    public RuleEditorView(IRuleManager ruleManager, ClassificationRule? rule, Action onSaved)
    {
        InitializeComponent();
        _ruleManager = ruleManager;
        _existingRule = rule;
        _onSaved = onSaved;

        if (_existingRule != null)
        {
            Title = "编辑分类规则";
            LoadRule(_existingRule);
        }
        else
        {
            Title = "新建分类规则";
            PrioritySlider.Value = 50;
        }

        PrioritySlider.ValueChanged += (s, e) =>
        {
            PriorityLabel.Text = ((int)e.NewValue).ToString();
        };
    }

    private void LoadRule(ClassificationRule rule)
    {
        NameEntry.Text = rule.Name;
        PatternEditor.Text = rule.NaturalLanguagePattern;
        PrioritySlider.Value = rule.Priority;
        PriorityLabel.Text = rule.Priority.ToString();

        if (rule.Condition != null)
        {
            if (rule.Condition.Extensions?.Count > 0)
                ExtensionsEntry.Text = string.Join(", ", rule.Condition.Extensions);

            if (rule.Condition.Keywords?.Count > 0)
                KeywordsEntry.Text = string.Join(", ", rule.Condition.Keywords);
        }
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("错误", "请输入规则名称", "确定");
            return;
        }

        if (string.IsNullOrWhiteSpace(PatternEditor.Text))
        {
            await DisplayAlert("错误", "请输入分类规则", "确定");
            return;
        }

        var condition = new RuleCondition();

        if (!string.IsNullOrWhiteSpace(ExtensionsEntry.Text))
        {
            condition.Extensions = ExtensionsEntry.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()).ToList();
        }

        if (!string.IsNullOrWhiteSpace(KeywordsEntry.Text))
        {
            condition.Keywords = KeywordsEntry.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()).ToList();
        }

        var rule = _existingRule ?? new ClassificationRule();
        rule.Name = NameEntry.Text;
        rule.NaturalLanguagePattern = PatternEditor.Text;
        rule.Priority = (int)PrioritySlider.Value;
        rule.Condition = condition;

        if (_existingRule == null)
        {
            await _ruleManager.CreateRuleAsync(rule);
        }
        else
        {
            await _ruleManager.UpdateRuleAsync(rule);
        }

        _onSaved?.Invoke();
        await Navigation.PopAsync();
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
