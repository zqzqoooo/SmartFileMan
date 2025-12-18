using System.Threading.Tasks;
using SmartFileMan.Contracts.Common;

namespace SmartFileMan.Sdk.Commands
{
    public interface IUndoableCommand
    {
        string Name { get; } // 操作名称 (如 "重命名文件")

        // 执行操作
        Task<OperationResult> ExecuteAsync();

        // 撤销操作 (后悔药)
        Task<OperationResult> UndoAsync();
    }
}