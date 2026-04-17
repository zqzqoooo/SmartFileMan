using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Common; // Added missing using

namespace SmartFileMan.Sdk.Commands
{
    public class BatchCommand : IUndoableCommand
    {
        private readonly List<IUndoableCommand> _commands = new();
        public string Name { get; }

        public BatchCommand(string name)
        {
            Name = name;
        }

        public void Add(IUndoableCommand command)
        {
            _commands.Add(command);
        }

        public async Task<OperationResult> ExecuteAsync()
        {
            // Batch usually executes one by one during the process,
            // but if we treat this as a "Redo", we execute all.
            foreach (var cmd in _commands)
            {
                var result = await cmd.ExecuteAsync();
                if (!result.IsSuccess) return result;
            }
            return OperationResult.Success();
        }

        public async Task<OperationResult> UndoAsync()
        {
            // Undo in reverse order
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                var result = await _commands[i].UndoAsync();
                if (!result.IsSuccess) return result;
            }
            return OperationResult.Success();
        }
    }
}
