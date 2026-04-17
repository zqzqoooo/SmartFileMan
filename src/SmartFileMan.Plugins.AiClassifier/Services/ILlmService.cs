using System.Threading;
using System.Threading.Tasks;
using SmartFileMan.Plugins.AiClassifier.Models;

namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// LLM 调用结果
/// LLM Call Result
/// </summary>
public class LlmCallResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawRequest { get; set; }
    public string? RawResponse { get; set; }
    public ClassificationResponse? Response { get; set; }
}

/// <summary>
/// LLM 服务接口
/// LLM Service Interface
/// </summary>
public interface ILlmService
{
    Task<LlmCallResult> ClassifyAsync(string ruleDescription, string filesText, CancellationToken cancellationToken = default);
    Task<LlmCallResult> TestConnectionAsync(CancellationToken cancellationToken = default);
    LlmConfig Config { get; set; }
}
