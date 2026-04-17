namespace SmartFileMan.Plugins.AiClassifier.Services;

/// <summary>
/// 文件树节点
/// File tree node
/// </summary>
public class FileTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public List<FileTreeNode> Children { get; set; } = new();
}

/// <summary>
/// 文件树服务接口
/// File Tree Service Interface
/// </summary>
public interface IFileTreeService
{
    /// <summary>
    /// 获取根目录下的文件树
    /// Get file tree under root directory
    /// </summary>
    Task<FileTreeNode> GetFileTreeAsync(string rootPath, int maxDepth = 3);

    /// <summary>
    /// 获取文件树文本表示（用于 LLM）
    /// Get file tree as text (for LLM)
    /// </summary>
    string GetFileTreeAsText(FileTreeNode node, int indent = 0);

    /// <summary>
    /// 获取文件列表文本表示（TSV格式）
    /// Get file list as text (TSV format)
    /// </summary>
    string GetFilesAsTsv(IReadOnlyList<Contracts.Models.IFileEntry> files);
}
