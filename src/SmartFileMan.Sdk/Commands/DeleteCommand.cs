using System;
using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Sdk.Commands
{
    public class DeleteCommand : IUndoableCommand
    {
        private readonly IFileEntry _file;
        private readonly string _originalPath;
        private string _recyclePath; // 临时存放路径

        public string Name => $"delete {_file.Name}";

        public DeleteCommand(IFileEntry file)
        {
            _file = file;
            _originalPath = file.FullPath;
        }

        public async Task<OperationResult> ExecuteAsync()
        {
            try
            {
                // 1. 准备“回收站”路径 (在系统临时目录下创建一个专属文件夹)
                var tempPath = Path.GetTempPath();
                var recycleBin = Path.Combine(tempPath, "SmartFileMan_RecycleBin");
                
                if (!Directory.Exists(recycleBin))
                    Directory.CreateDirectory(recycleBin);

                // 使用 GUID 防止文件名冲突
                var uniqueName = $"{Guid.NewGuid()}_{_file.Name}";
                _recyclePath = Path.Combine(recycleBin, uniqueName);

                // 2. 移动文件到回收站 (模拟删除)
                await Task.Run(() => File.Move(_originalPath, _recyclePath));

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"删除失败: {ex.Message}", ex);
            }
        }

        public async Task<OperationResult> UndoAsync()
        {
            try
            {
                if (File.Exists(_recyclePath))
                {
                    // 确保原目录还存在
                    var originalDir = Path.GetDirectoryName(_originalPath);
                    if (!string.IsNullOrEmpty(originalDir) && !Directory.Exists(originalDir))
                    {
                        Directory.CreateDirectory(originalDir);
                    }

                    // 移回原位
                    await Task.Run(() => File.Move(_recyclePath, _originalPath));
                    return OperationResult.Success("已撤销删除");
                }
                return OperationResult.Fail("无法撤销：回收站文件已丢失");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"撤销删除失败: {ex.Message}", ex);
            }
        }
    }
}
