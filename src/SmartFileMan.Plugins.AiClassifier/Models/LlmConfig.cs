namespace SmartFileMan.Plugins.AiClassifier.Models;

/// <summary>
/// LLM 提供者类型
/// LLM Provider Type
/// </summary>
public enum LlmProviderType
{
    OpenAI,
    MiniMax
}

/// <summary>
/// LLM 配置
/// LLM Configuration
/// </summary>
public class LlmConfig
{
    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://api.minimaxi.com/v1/text/chatcompletion_v2";

    public LlmProviderType Provider { get; set; } = LlmProviderType.MiniMax;

    public string Model { get; set; } = "MiniMax-M2.7-highspeed";

    public int MaxTokens { get; set; } = 2048;

    public double Temperature { get; set; } = 1.0;

    public double TopP { get; set; } = 0.95;

    public string CustomPrompt { get; set; } = "请自行根据文件类型和名称推断，将文件分类到合适的文件夹中";
}
