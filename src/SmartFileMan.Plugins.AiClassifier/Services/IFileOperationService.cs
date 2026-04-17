namespace SmartFileMan.Plugins.AiClassifier.Services;

public class FileOperation
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string OperationType { get; set; } = "MOVE";
}

public interface IFileOperationService
{
    Task<bool> MoveFileAsync(string sourcePath, string targetDirectory, CancellationToken cancellationToken = default);
    Task<bool> RenameFileAsync(string sourcePath, string newName, CancellationToken cancellationToken = default);
    Task<FileOperation?> PlanMoveAsync(string sourcePath, string targetDirectory);
    Task<FileOperation?> PlanRenameAsync(string sourcePath, string newName);
    bool ValidatePath(string path);
    bool IsPathSafe(string path, string baseDirectory);
}
