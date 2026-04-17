using System.Threading.Tasks;
using SmartFileMan.Contracts.Common;

namespace SmartFileMan.Sdk.Commands
{
    public interface IUndoableCommand
    {
        string Name { get; }

        Task<OperationResult> ExecuteAsync();

        Task<OperationResult> UndoAsync();
    }
}