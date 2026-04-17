using System.Text.Json;

namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// 解析服务实现
/// Parser Service Implementation
/// </summary>
public class ParserService : IParserService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Models.ParseResult Parse(string jsonResponse)
    {
        var result = new Models.ParseResult { IsValid = true };

        Models.ClassificationResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<Models.ClassificationResponse>(jsonResponse, JsonOptions);
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add($"JSON 解析失败: {ex.Message}");
            return result;
        }

        if (response == null)
        {
            result.IsValid = false;
            result.Errors.Add("响应为空");
            return result;
        }

        if (response.Operations == null || response.Operations.Count == 0)
        {
            result.Warnings.Add("没有分类操作");
            return result;
        }

        foreach (var op in response.Operations)
        {
            if (op.Operation != "MOVE" && op.Operation != "RENAME")
            {
                result.Warnings.Add($"无效操作类型: {op.Operation}");
                continue;
            }

            if (op.Confidence < 0 || op.Confidence > 100)
            {
                op.Confidence = Math.Clamp(op.Confidence, 0, 100);
            }

            result.Operations.Add(op);
        }

        return result;
    }

    public List<Models.PathConflict> DetectConflicts(List<Models.ClassificationOperation> operations)
    {
        var conflicts = new List<Models.PathConflict>();
        var targetMap = new Dictionary<string, List<string>>();

        foreach (var op in operations)
        {
            var target = Path.Combine(op.TargetDirectory, op.SourcePath);
            if (!targetMap.ContainsKey(target))
                targetMap[target] = new List<string>();

            targetMap[target].Add(op.SourcePath);
        }

        foreach (var kvp in targetMap.Where(kv => kv.Value.Count > 1))
        {
            var files = kvp.Value;
            for (int i = 0; i < files.Count; i++)
            {
                for (int j = i + 1; j < files.Count; j++)
                {
                    conflicts.Add(new Models.PathConflict
                    {
                        File1 = files[i],
                        File2 = files[j],
                        ConflictType = "duplicate_target"
                    });
                }
            }
        }

        return conflicts;
    }
}
