using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Sdk.Commands
{
    public class MoveCommand : IUndoableCommand
    {
        private readonly IFileEntry _file;
        private readonly string _destinationFolder;
        private string _originalPath;
        private string? _newPath;
                                               
        public string Name => $"移动 {_file.Name} 到 {_destinationFolder}";

        public MoveCommand(IFileEntry file, string destinationFolder)
        {
            _file = file;
            _destinationFolder = destinationFolder;
            _originalPath = file.FullPath;
        }

        public async Task<OperationResult> ExecuteAsync()
        {
            try
            {
                // 1. 确保目标文件夹存在
                if (!Directory.Exists(_destinationFolder))
                {
                    Directory.CreateDirectory(_destinationFolder);
                }

                _newPath = Path.Combine(_destinationFolder, _file.Name);

                // 2. 检查冲突
                if (File.Exists(_newPath))
                {
                    return OperationResult.Fail($"目标位置已存在同名文件: {_newPath}");
                }

                // 3. 移动
                await Task.Run(() => File.Move(_originalPath, _newPath));

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"移动失败: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> UndoAsync()
        {
            try
            {
                if (File.Exists(_newPath))
                {
                    // 撤销：移回原位
                    // 注意：如果原文件夹被删了，这里可能需要重建原文件夹逻辑
                    string originalDir = Path.GetDirectoryName(_originalPath)!;
                    if (!Directory.Exists(originalDir)) Directory.CreateDirectory(originalDir);

                    await Task.Run(() => File.Move(_newPath, _originalPath));
                    return OperationResult.Success("已撤销移动");
                }
                return OperationResult.Fail("文件丢失，无法撤销");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"撤销移动失败: {ex.Message}", ex);
            }
        }
    }
}