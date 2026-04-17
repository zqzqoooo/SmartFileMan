using System.IO;

namespace SmartFileMan.Plugins.AiClassifier.Services;

public class FileTreeService : IFileTreeService
{
    public async Task<FileTreeNode> GetFileTreeAsync(string rootPath, int maxDepth = 3)
    {
        return await Task.Run(() =>
        {
            var rootDir = new DirectoryInfo(rootPath);
            return BuildTree(rootDir, 0, maxDepth);
        });
    }

    private FileTreeNode BuildTree(DirectoryInfo dir, int currentDepth, int maxDepth)
    {
        var node = new FileTreeNode
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            RelativePath = dir.FullName,
            IsDirectory = true
        };

        if (currentDepth >= maxDepth)
            return node;

        try
        {
            foreach (var subDir in dir.GetDirectories())
            {
                if ((subDir.Attributes & FileAttributes.Hidden) != 0)
                    continue;

                node.Children.Add(BuildTree(subDir, currentDepth + 1, maxDepth));
            }

            foreach (var file in dir.GetFiles())
            {
                if ((file.Attributes & FileAttributes.Hidden) != 0)
                    continue;

                node.Children.Add(new FileTreeNode
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    RelativePath = file.FullName,
                    IsDirectory = false,
                    Extension = file.Extension.ToLowerInvariant(),
                    Size = file.Length,
                    LastModified = file.LastWriteTime
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
        }

        return node;
    }

    public string GetFileTreeAsText(FileTreeNode node, int indent = 0)
    {
        var sb = new System.Text.StringBuilder();
        var prefix = new string(' ', indent * 2);

        if (node.IsDirectory)
        {
            sb.AppendLine($"{prefix}[D] {node.Name}/");
            foreach (var child in node.Children)
            {
                sb.Append(GetFileTreeAsText(child, indent + 1));
            }
        }
        else
        {
            sb.AppendLine($"{prefix}[F] {node.Name} ({node.Extension})");
        }

        return sb.ToString();
    }

    public string GetFilesAsTsv(IReadOnlyList<Contracts.Models.IFileEntry> files)
    {
        var lines = new List<string> { "FileName\tRelativePath\tExtension\tSize" };

        foreach (var file in files)
        {
            lines.Add($"{file.Name}\t{file.FullPath}\t{file.Extension}\t0");
        }

        return string.Join("\n", lines);
    }
}
