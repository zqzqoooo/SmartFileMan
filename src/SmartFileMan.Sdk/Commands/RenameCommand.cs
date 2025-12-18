using System;
using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;

namespace SmartFileMan.Sdk.Commands
{
    public class RenameCommand : IUndoableCommand
    {
        private readonly IFileEntry _file;
        private readonly string _newName;
        private readonly string _originalName;
        private string _originalPath;
        private string _newPath = string.Empty;

        public string Name => $"重命名 {_originalName} -> {_newName}";

        public RenameCommand(IFileEntry file, string newName)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _newName = newName ?? throw new ArgumentNullException(nameof(newName));
            _originalName = file.Name; // 记录旧名字
            _originalPath = file.FullPath;
        }

        public async Task<OperationResult> ExecuteAsync()
        {
            try
            {
                var folder = Path.GetDirectoryName(_originalPath);
                if (string.IsNullOrEmpty(folder))
                    return OperationResult.Fail("无法计算目标文件夹路径");

                _newPath = Path.Combine(folder, _newName);

                // 同名文件存在时覆盖可能不安全，这里检查并失败以保守处理
                if (File.Exists(_newPath))
                {
                    return OperationResult.Fail("目标文件已存在");
                }

                await Task.Run(() => File.Move(_originalPath, _newPath));

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"重命名失败: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> UndoAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_newPath))
                    return OperationResult.Fail("无法撤销：未记录新路径");

                if (File.Exists(_newPath))
                {
                    await Task.Run(() => File.Move(_newPath, _originalPath));
                    return OperationResult.Success("已撤销重命名");
                }

                return OperationResult.Fail("无法撤销：文件已丢失");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"撤销失败: {ex.Message}", ex);
            }
        }
    }
}
