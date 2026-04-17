using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Sdk.Commands
{
    public class ModifyContentCommand : IUndoableCommand
    {
        private readonly IFileEntry _file;
        private readonly string _backupPath;
        // 我们可能需要保留修改后的“新状态”备份以便 Redo，但目前只做 Undo
        // 为了支持 Redo，Undo 时需要把“新状态”也备份一下，或者只是简单的 swap。
        // 简化起见，Undo = 用旧备份覆盖当前。

        public string Name => $"修改文件内容: {_file.Name}";

        public ModifyContentCommand(IFileEntry file, string backupPath)
        {
            _file = file;
            _backupPath = backupPath;
        }

        public Task<OperationResult> ExecuteAsync()
        {
            // Execute 已经在 SafeContext.ModifyContentAsync 里被业务逻辑“做完”了。
            // 这种命令比较特殊，它的 Execute 通常是“重做”。
            // 目前 SafeContext 逻辑是 Do Action -> Push Command。
            // 如果是 Redo，我们需要再次运行 modifyAction? 或者是存了“修改后”的副本？
            // 鉴于 modifyAction 可能不可重现（比如随机数），通常 Redo 需要保存修改后的快照。
            // 为了节省空间，简单的实现暂不支持 Redo，或者暂定 Execute = Success。
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> UndoAsync()
        {
            try
            {
                if (File.Exists(_backupPath))
                {
                    // 恢复备份
                    // 也许我们需要备份“修改后”的文件以便 Redo？目前先只管 Undo。
                    File.Copy(_backupPath, _file.FullPath, overwrite: true);
                    return Task.FromResult(OperationResult.Success("已恢复原文件内容"));
                }
                return Task.FromResult(OperationResult.Fail("备份文件丢失，无法撤销"));
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(OperationResult.Fail($"撤销失败: {ex.Message}"));
            }
        }
    }
}
