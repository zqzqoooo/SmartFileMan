using SmartFileMan.Plugins.MovieCollection.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 重命名服务接口
/// Rename service interface for media files
/// </summary>
public interface IRenameService
{
    /// <summary>
    /// 生成重命名提案
    /// Generate rename proposals for batch files
    /// </summary>
    List<RenameProposal> GenerateProposals(
        List<MediaFileIndex> files,
        MediaInfo mediaInfo,
        List<SeasonInfo> seasons,
        string template,
        string? targetFolder = null);

    /// <summary>
    /// 格式化集数标题
    /// Format episode title with season and episode number
    /// </summary>
    string FormatEpisodeTitle(string template, MediaInfo mediaInfo, EpisodeInfo episode);

    /// <summary>
    /// 获取文件的正确扩展名
    /// Get the correct file extension
    /// </summary>
    string GetExtension(string originalPath);

    /// <summary>
    /// 验证文件名是否合法
    /// Validate if filename is valid
    /// </summary>
    bool IsValidFileName(string fileName);
}
