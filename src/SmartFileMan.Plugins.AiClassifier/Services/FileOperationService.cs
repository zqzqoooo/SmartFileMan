using System.IO;
using System.Linq;

namespace SmartFileMan.Plugins.AiClassifier.Services;

public class FileOperationService : IFileOperationService
{
    public async Task<bool> MoveFileAsync(string sourcePath, string targetDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(sourcePath))
                return false;

            var fileName = Path.GetFileName(sourcePath);
            var targetPath = Path.Combine(targetDirectory, fileName);

            if (!ValidatePath(targetPath))
                return false;

            var targetDir = new DirectoryInfo(targetDirectory);
            if (!targetDir.Exists)
                targetDir.Create();

            await Task.Run(() => File.Move(sourcePath, targetPath), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RenameFileAsync(string sourcePath, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(sourcePath))
                return false;

            var directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(directory))
                return false;

            var targetPath = Path.Combine(directory, newName);

            if (!ValidatePath(targetPath))
                return false;

            await Task.Run(() => File.Move(sourcePath, targetPath), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<FileOperation?> PlanMoveAsync(string sourcePath, string targetDirectory)
    {
        if (!ValidatePath(sourcePath) || !ValidatePath(targetDirectory))
            return Task.FromResult<FileOperation?>(null);

        var fileName = Path.GetFileName(sourcePath);
        var targetPath = Path.Combine(targetDirectory, fileName);

        var operation = new FileOperation
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            OperationType = "MOVE"
        };

        return Task.FromResult<FileOperation?>(operation);
    }

    public Task<FileOperation?> PlanRenameAsync(string sourcePath, string newName)
    {
        if (!ValidatePath(sourcePath))
            return Task.FromResult<FileOperation?>(null);

        var directory = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrEmpty(directory))
            return Task.FromResult<FileOperation?>(null);

        var targetPath = Path.Combine(directory, newName);

        if (!ValidatePath(targetPath))
            return Task.FromResult<FileOperation?>(null);

        var operation = new FileOperation
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            OperationType = "RENAME"
        };

        return Task.FromResult<FileOperation?>(operation);
    }

    public bool ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            return !fullPath.Contains("..");
        }
        catch
        {
            return false;
        }
    }

    public bool IsPathSafe(string path, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(baseDirectory))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBaseDir = Path.GetFullPath(baseDirectory);
            return fullPath.StartsWith(fullBaseDir, System.StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
