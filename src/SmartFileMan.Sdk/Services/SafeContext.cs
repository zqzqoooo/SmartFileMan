using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Sdk.Commands;
using SmartFileMan.Contracts;

namespace SmartFileMan.Sdk.Services
{
    public class SafeContext
    {
        private readonly IInteractionProvider _ui;
        // 撤销栈：后进先出
        private readonly Stack<IUndoableCommand> _undoStack = new();
        // 重做栈 (可选功能)
        private readonly Stack<IUndoableCommand> _redoStack = new();
        public SafeContext(IInteractionProvider ui)
        {
            _ui = ui;
        }

        // --- 核心 API：执行命令 ---

        // 1. 修改这个私有方法的签名，从 Task 改为 Task<OperationResult>
        private async Task<OperationResult> ExecuteCommandAsync(IUndoableCommand command, bool requireConfirm = false)
        {
            // 1. 安全检查 / 二次确认
            if (requireConfirm)
            {
                bool agreed = await _ui.ConfirmAsync("安全警告", $"插件请求执行操作：{command.Name}，是否允许？");
                if (!agreed) return OperationResult.Fail("用户取消操作"); // 返回取消结果
            }

            // 2. 执行
            var result = await command.ExecuteAsync();

            if (result.IsSuccess)
            {
                // 3. 成功后压入撤销栈
                _undoStack.Push(command);
                _redoStack.Clear();
                await _ui.ToastAsync($"执行成功: {command.Name}");
            }
            else
            {
                await _ui.AlertErrorAsync("操作失败", result.Message);
            }

            // 【关键修复】这里必须把结果返回去
            return result;
        }

        // --- 暴露给开发者的 API ---

        public async Task RenameAsync(IFileEntry file, string newName)
        {
            var command = new RenameCommand(file, newName);
            // 重命名通常不需要二次确认，除非是敏感文件，这里默认为 false
            await ExecuteCommandAsync(command, requireConfirm: false);
        }

        // 这里的 Delete 以后我们会实现 MoveToRecycleBinCommand
        // public async Task DeleteAsync(IFileEntry file) ...

        // --- 撤销功能 (绑定到 UI 按钮) ---

        public async Task UndoLastActionAsync()
        {
            if (_undoStack.Count == 0) return;

            var command = _undoStack.Pop();
            var result = await command.UndoAsync();

            if (result.IsSuccess)
            {
                _redoStack.Push(command);
                await _ui.ToastAsync($"已撤销: {command.Name}");
            }
            else
            {
                await _ui.AlertErrorAsync("撤销失败", result.Message);
                // 失败通常不压回栈，或者根据策略处理
            }
        }

        // 在 SafeContext 类中添加

        public async Task<OperationResult> MoveAsync(IFileEntry file, string destinationFolder)
        {
            var command = new MoveCommand(file, destinationFolder);
            // 移动操作通常需要记录 Undo，但不一定每次都要弹窗确认，除非是跨磁盘
            // 这里设为 false，依靠 Undo 来保证安全
            return await ExecuteCommandAsync(command, requireConfirm: false);
        }
    }
}