using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Sdk.Commands;
using System;
using System.IO;

namespace SmartFileMan.Sdk.Services
{
    public class SafeContext
    {
        private readonly IInteractionProvider _ui;
        // 撤销栈：后进先出
        private readonly Stack<IUndoableCommand> _undoStack = new();
        // 重做栈 (可选功能)
        private readonly Stack<IUndoableCommand> _redoStack = new(); // Not fully used yet

        private BatchCommand? _currentBatch;
        
        public int MaxUndoSteps { get; set; } = 50;

        /// <summary>
        /// 全局日志广播事件 (供 Debug 插件订阅)
        /// Global Log Broadcast Event (For Debug Plugin subscription)
        /// Params: Category, Code, Message
        /// </summary>
        public event Action<string, string, string>? SystemLogBroadcast;

        public SafeContext(IInteractionProvider ui)
        {
            _ui = ui;
        }

        public void BroadcastLog(string category, string code, string message)
        {
            SystemLogBroadcast?.Invoke(category, code, message);
        }

        public void BeginBatch(string batchName)
        {
            if (_currentBatch != null) 
            {
                // Nested batch not supported for simplicity, or just flatten.
                // Let's assume flat for now or ignore.
                // Or commit current and start new?
                // Let's auto-commit for safety or throw.
                // Ignoring nested for now.
                return;
            }
            _currentBatch = new BatchCommand(batchName);
        }

        public void CommitBatch()
        {
            if (_currentBatch != null)
            {
                if (_currentBatch.Name != null) // Check if has commands? BatchCommand.Count implementation needed? 
                {
                     // Only push if it has commands? 
                     // Let's assume user handled logic.
                     _undoStack.Push(_currentBatch);
                     _redoStack.Clear();
                }
                _currentBatch = null;
            }
        }

        public async Task<OperationResult> ExecuteCommandAsync(IUndoableCommand command, bool requireConfirm = false)
        {
            if (requireConfirm)
            {
                bool agreed = await _ui.ConfirmAsync("Safety Warning", $"Plugin requests to perform an action：{command.Name}，Is it allowed？");
                if (!agreed) return OperationResult.Fail("用户取消操作");
            }

            var result = await command.ExecuteAsync();

            if (result.IsSuccess)
            {
                if (_currentBatch != null)
                {
                    _currentBatch.Add(command);
                }
                else
                {
                    _undoStack.Push(command);
                    if (_undoStack.Count > MaxUndoSteps)
                    {}
                    _redoStack.Clear();
                }
                // Optional: Toast for single command only if not batching? 
                // Or let caller handle batch toast.
                if (_currentBatch == null) await _ui.ToastAsync($"执行成功: {command.Name}");
            }
            else
            {
                await _ui.AlertErrorAsync("操作失败", result.Message);
            }

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
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                await command.UndoAsync();
                
                // _redoStack.Push(command); // Implementing proper Redo logic requires decoupling Execute from Push.
                // For now, simple Undo.
                
                await _ui.ToastAsync($"已撤销: {command.Name}");
            }
            else
            {
                await _ui.ToastAsync("没有可撤销的操作");
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

        // 新增：安全删除
        public async Task<OperationResult> DeleteAsync(IFileEntry file)
        {
            var command = new DeleteCommand(file);
            // 删除是危险操作，默认建议开启二次确认 (requireConfirm: true)
            // 但为了流畅体验，如果支持完美撤销，也可以设为 false。这里我们保守一点设为 true。
            return await ExecuteCommandAsync(command, requireConfirm: true);
        }

        // 新增：安全修改文件内容
        public async Task<OperationResult> ModifyContentAsync(IFileEntry file, Func<string, Task<bool>> modifyAction)
        {
            // 1. 创建备份
            string backupPath = Path.Combine(Path.GetTempPath(), "SmartFileMan_Backup", $"{Guid.NewGuid()}_{file.Name}");
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            
            try 
            {
                // 复制原文件作为备份
                File.Copy(file.FullPath, backupPath, overwrite: true);

                // 2. 执行修改操作 (传入 FullPath 给插件)
                bool success = await modifyAction(file.FullPath);

                if (!success)
                {
                    // 如果插件自己返回失败，恢复备份并清理
                    File.Copy(backupPath, file.FullPath, overwrite: true);
                    return OperationResult.Fail("插件执行修改逻辑失败，已回滚");
                }

                // 3. 记录撤销命令
                var command = new ModifyContentCommand(file, backupPath);
                
                await ExecuteCommandAsync(command, requireConfirm: false);
                return OperationResult.Success("内容修改成功");
            }
            catch (Exception ex)
            {
                // 发生异常，尝试回滚
                if (File.Exists(backupPath))
                {
                    try { File.Copy(backupPath, file.FullPath, overwrite: true); } catch { }
                }
                return OperationResult.Fail($"修改失败: {ex.Message}", ex);
            }
        }
    }
}