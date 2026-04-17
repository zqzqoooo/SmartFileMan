using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Contracts.Core;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 批次分析模块接口
/// Batch analysis module interface for media file processing
/// </summary>
public interface IBatchModule
{
    /// <summary>
    /// 分析批次中的所有文件
    /// Analyze all files in the batch context
    /// </summary>
    Task<List<MediaFileIndex>> AnalyzeBatchAsync(BatchContext context);

    /// <summary>
    /// 从文件名提取集数信息
    /// Extract episode information from filename
    /// </summary>
    EpisodeInfo? ExtractEpisodeInfo(string fileName);

    /// <summary>
    /// 识别媒体类型
    /// Recognize media type (movie or TV show)
    /// </summary>
    string RecognizeMediaType(string fileName);

    /// <summary>
    /// 生成清理后的搜索关键词
    /// Generate cleaned search keyword from filename
    /// </summary>
    string GenerateSearchKeyword(string fileName);
}
