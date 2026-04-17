namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 重命名提案
/// Rename proposal for file operations
/// </summary>
public class RenameProposal
{
    /// <summary>原始文件路径</summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>原始文件名</summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>提案的新名称（不含路径）</summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>提案的新路径（含文件名）</summary>
    public string NewPath { get; set; } = string.Empty;

    /// <summary>关联的媒体信息</summary>
    public MediaInfo? MediaInfo { get; set; }

    /// <summary>提案得分（0-100）</summary>
    public int Score { get; set; }

    /// <summary>提案理由</summary>
    public string Reason { get; set; } = string.Empty;
}
