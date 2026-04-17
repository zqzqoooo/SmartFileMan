namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// 解析服务接口
/// Parser Service Interface
/// </summary>
public interface IParserService
{
    /// <summary>
    /// 解析并验证 LLM 响应
    /// Parse and validate LLM response
    /// </summary>
    Models.ParseResult Parse(string jsonResponse);

    /// <summary>
    /// 检测路径冲突
    /// Detect path conflicts
    /// </summary>
    List<Models.PathConflict> DetectConflicts(List<Models.ClassificationOperation> operations);
}
