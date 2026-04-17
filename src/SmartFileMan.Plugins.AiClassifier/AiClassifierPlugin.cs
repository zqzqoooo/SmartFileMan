using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.UI;
using SmartFileMan.Sdk;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SmartFileMan.Plugins.AiClassifier.Models;
using SmartFileMan.Plugins.AiClassifier.Services;
using SmartFileMan.Plugins.AiClassifier.Views;

namespace SmartFileMan.Plugins.AiClassifier;

/// <summary>
/// AI 分类插件主类
/// AI Classifier Plugin Main Class
/// </summary>
public class AiClassifierPlugin : PluginBase, IFilePlugin, IPluginUI
{
    public override string Id => "com.smartfileman.plugins.aiclassifier";
    public override string DisplayName => "AI Intelligent Classification";
    public override string Description => "基于 LLM 的语义文件分类插件";
    public override PluginType Type => PluginType.General;
    public override string Version => "1.0.0";

    private IRuleManager _ruleManager = null!;
    private ILlmService _llmService = null!;
    private IParserService _parserService = null!;

    private List<IFileEntry> _currentBatchFiles = new();
    private ClassificationResponse? _batchClassificationResult = null;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _ruleManager = new RuleManager(Storage!);
        _llmService = new LlmService();
        _parserService = new ParserService();

        var config = Storage!.Load<Models.LlmConfig>("LlmConfig");
        if (config != null)
        {
            _llmService.Config = config;
        }
    }

    /// <summary>
    /// 阶段零：收集批次文件上下文
    /// Phase 0: Collect batch file context
    /// </summary>
    public override async Task AnalyzeBatchAsync(BatchContext context)
    {
        _currentBatchFiles = context.AllFiles.ToList();

        if (_currentBatchFiles.Count == 0) return;

        _batchClassificationResult = null;

        var filesText = string.Join("\n", _currentBatchFiles.Select(f =>
            $"{f.Name}\t{f.FullPath}\t{f.Extension}"));

        // Directly use custom prompt command
        var ruleDescription = _llmService.Config.CustomPrompt;

        if (string.IsNullOrWhiteSpace(ruleDescription))
        {
            ruleDescription = "General classification: Please infer based on the file type and name, and categorize it into the appropriate folder";
        }

        var llmResult = await _llmService.ClassifyAsync(ruleDescription, filesText);
        if (llmResult.Success)
        {
            _batchClassificationResult = llmResult.Response;
            System.Diagnostics.Debug.WriteLine($"[AiClassifier] Successfully classified batch of {context.AllFiles.Count} files.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[AiClassifier] Failed to classify batch: {llmResult.ErrorMessage}");
        }
    }

    /// <summary>
    /// 阶段二：竞价 - 返回分类提案
    /// Phase 2: Bid - Return classification proposal
    /// </summary>
    public override Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
    {
        if (_batchClassificationResult == null || _batchClassificationResult.Status != "success")
        {
            System.Diagnostics.Debug.WriteLine($"[AiClassifier] Rejected bid for {file.Name}: No successful batch classification result.");
            return Task.FromResult<RouteProposal?>(null);
        }

        var parseResult = new ParseResult
        {
            IsValid = true,
            Operations = _batchClassificationResult.Operations
        };

        // Note: The LLM output operations.SourcePath might just be the file name or a partial path.
        // E.g., OpenAI might output just the file name instead of the full path
        var operation = parseResult.Operations
            .FirstOrDefault(op => op.SourcePath.Contains(file.Name, System.StringComparison.InvariantCultureIgnoreCase) || file.Name.Equals(Path.GetFileName(op.SourcePath), System.StringComparison.InvariantCultureIgnoreCase));

        if (operation == null)
        {
            System.Diagnostics.Debug.WriteLine($"[AiClassifier] Rejected bid for {file.Name}: No operation mapped from LLM.");
            return Task.FromResult<RouteProposal?>(null);
        }

        var conflicts = _parserService.DetectConflicts(parseResult.Operations);
        bool hasConflict = conflicts.Any(c =>
            c.File1.Contains(file.Name) || c.File2.Contains(file.Name));

        string resolveTargetDir = operation.TargetDirectory;
        if (!Path.IsPathRooted(resolveTargetDir))
        {
            var fileDir = Path.GetDirectoryName(file.FullPath) ?? string.Empty;
            resolveTargetDir = Path.Combine(fileDir, resolveTargetDir);
        }
        resolveTargetDir = Path.GetFullPath(resolveTargetDir);

        string targetPath = Path.Combine(resolveTargetDir, file.Name);
        int score = hasConflict ? 30 : 70; // 调整为 70 分

        System.Diagnostics.Debug.WriteLine($"[AiClassifier] Proposing destination for {file.Name} to {targetPath} with score {score}");

        return Task.FromResult<RouteProposal?>(new RouteProposal(targetPath, score, $"AI分类: {operation.TargetDirectory}"));
    }

    public View GetView()
    {
        return new AiClassifierEntryView(this);
    }

    public Models.LlmConfig GetLlmConfig() => _llmService.Config;

    public void SaveLlmConfig(Models.LlmConfig config)
    {
        _llmService.Config = config;
        Storage!.Save("LlmConfig", config);
    }

    public IRuleManager GetRuleManager() => _ruleManager;

    public async Task<Services.LlmCallResult> TestLlmConnectionAsync()
    {
        return await _llmService.TestConnectionAsync();
    }
}
