using System.Net.Http;
using System.Text;
using System.Text.Json;
using SmartFileMan.Plugins.AiClassifier.Models;

namespace SmartFileMan.Plugins.AiClassifier.Services;

public class LlmService : ILlmService
{
    private static readonly HttpClient _httpClient = new();

    public LlmConfig Config { get; set; } = new();

    private const string SystemPromptTemplate = @"你是一个文件分类助手。请根据用户提供的分类规则，对给定的文件列表进行分类。

## 分类规则
{rule_description}

## 文件列表
{files_text}

## 输出要求
严格按照以下 JSON Schema 输出，不要输出其他内容：
```json
{
  ""operations"": [
    {
      ""sourcePath"": ""相对路径"",
      ""targetDirectory"": ""目标目录相对路径"",
      ""operation"": ""MOVE"",
      ""confidence"": 85
    }
  ],
  ""status"": ""success"",
  ""ruleSummary"": ""规则理解摘要""
}
```

## 注意事项
1. 只输出 MOVE 或 RENAME 操作
2. targetDirectory 必须是相对路径，不能包含 .. 或绝对路径
3. confidence 表示分类置信度 (0-100)
4. 只为匹配规则的文件生成操作
5. 直接输出 JSON，不要包含任何思考过程或额外文字";

    public async Task<LlmCallResult> ClassifyAsync(
        string ruleDescription,
        string filesText,
        CancellationToken cancellationToken = default)
    {
        var result = new LlmCallResult();

        try
        {
            var userPrompt = SystemPromptTemplate
                .Replace("{rule_description}", ruleDescription)
                .Replace("{files_text}", filesText);

            var messages = new object[]
            {
                new { role = "system", content = "你是一个文件分类助手，只输出 JSON，不要输出其他内容。" },
                new { role = "user", content = userPrompt }
            };

            string requestBody;
            if (Config.Provider == LlmProviderType.MiniMax)
            {
                var miniMaxRequest = new
                {
                    model = Config.Model ?? "MiniMax-M2.7",
                    max_tokens = Config.MaxTokens > 0 ? Config.MaxTokens : 1000,
                    system = "你是一个文件分类助手，只输出 JSON，不要输出其他内容。",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new[]
                            {
                                new { type = "text", text = userPrompt }
                            }
                        }
                    },
                    temperature = Config.Temperature,
                    stream = false
                };
                requestBody = JsonSerializer.Serialize(miniMaxRequest);
            }
            else
            {
                var openAiRequest = new
                {
                    model = Config.Model,
                    messages = messages,
                    temperature = Config.Temperature,
                    max_tokens = Config.MaxTokens
                };
                requestBody = JsonSerializer.Serialize(openAiRequest);
            }

            result.RawRequest = $"[{HttpMethod.Post} {Config.Endpoint}]\n{requestBody}";

            var request = new HttpRequestMessage(HttpMethod.Post, Config.Endpoint)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Setup Auth Headers / 设置验证头
            if (Config.Provider == LlmProviderType.MiniMax)
            {
                // Use Anthropic compatible headers / 使用 Anthropic 兼容器格式请求头
                request.Headers.Add("x-api-key", Config.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                // 稳妥起见也保留 Bearer 等 OpenAI 风格 / Retain fallback for standard bearer
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.ApiKey);
            }
            else
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.ApiKey);
            }

            // 调试日志: 记录请求详情 / Debug log: record request details
            System.Diagnostics.Debug.WriteLine($"[LlmService] Sending request to {Config.Endpoint}");
            System.Diagnostics.Debug.WriteLine($"[LlmService] Request Body: {requestBody}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            // 调试日志: 记录响应详情 / Debug log: record response details
            System.Diagnostics.Debug.WriteLine($"[LlmService] Response Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[LlmService] Response Body: {jsonResponse}");

            result.RawResponse = jsonResponse;

            if (!response.IsSuccessStatusCode)
            {
                result.Success = false;
                result.ErrorMessage = $"HTTP {response.StatusCode}: {jsonResponse}";
                return result;
            }

            var classificationResponse = ParseResponse(jsonResponse);
            if (classificationResponse != null)
            {
                result.Success = true;
                result.Response = classificationResponse;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "无法解析 LLM 响应";
                System.Diagnostics.Debug.WriteLine($"[LlmService] ParseError: {result.ErrorMessage}, RawResponse: {jsonResponse}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[LlmService] Exception: {ex}");
        }

        return result;
    }

    public async Task<LlmCallResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await ClassifyAsync(
            "测试连接",
            "FileName\tRelativePath\tExtension\ntest.txt\ttest.txt\t.txt",
            cancellationToken);
    }

    private ClassificationResponse? ParseResponse(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            string? contentStr = null;

            if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                // 处理 Anthropic 的响应格式 / Handle Anthropic response format
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var typeValue) && typeValue.GetString() == "text")
                    {
                        if (block.TryGetProperty("text", out var textValue))
                        {
                            contentStr = textValue.GetString();
                            break; // 只要第一个文本块 / Stop at first text response
                        }
                    }
                }
            }
            else if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                // 处理 OpenAI 的响应格式 / Handle OpenAI response format
                var firstChoice = choices[0];

                if (firstChoice.ValueKind == JsonValueKind.Object &&
                    firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content))
                    {
                        contentStr = content.GetString();
                    }
                }
            }

            if (string.IsNullOrEmpty(contentStr))
            {
                if (root.TryGetProperty("error", out var error))
                {
                    contentStr = error.GetRawText();
                }
            }

            if (string.IsNullOrEmpty(contentStr))
                return null;

            contentStr = contentStr.Trim();

            if (contentStr.Contains("```json"))
            {
                var start = contentStr.IndexOf("```json") + 7;
                var end = contentStr.LastIndexOf("```");
                if (end > start)
                {
                    contentStr = contentStr.Substring(start, end - start).Trim();
                }
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ClassificationResponse>(contentStr, options);
        }
        catch
        {
            return null;
        }
    }
}
