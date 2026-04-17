using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Contracts.Storage;
using SmartFileMan.Contracts.UI;
using SmartFileMan.Sdk.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SmartFileMan.Sdk
{
    /// <summary>
    /// 插件的基类：提供安全上下文 (SafeContext) 和存储 (Storage) 的自动管理
    /// Base class for plugins: Provides automatic management of SafeContext and Storage
    /// </summary>
    public abstract class PluginBase : IFilePlugin
    {
        // --- 插件基础信息 / Basic Plugin Information ---

        // 插件唯一标识符
        // Unique identifier for the plugin
        public abstract string Id { get; }

        // 插件显示名称
        // Display name of the plugin
        public abstract string DisplayName { get; }

        // 插件功能描述
        // Functional description of the plugin
        public abstract string Description { get; }

        // 插件版本号（默认为 1.0.0）
        // Plugin version number (defaults to 1.0.0)
        public virtual string Version => "1.0.0";

        // 插件启用状态
        // Whether the plugin is enabled
        public virtual bool IsEnabled { get; set; } = true;

        // --- 新增：IFilePlugin 实现 / New: IFilePlugin Implementation ---

        /// <summary>
        /// 默认插件类型为通用
        /// Default plugin type is General
        /// </summary>
        public virtual PluginType Type => PluginType.General;

        /// <summary>
        /// Phase Zero: Analyze batch. Default does nothing.
        /// </summary>
        public virtual Task AnalyzeBatchAsync(BatchContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 阶段一：默认不做任何事
        /// Phase 1: Do nothing by default
        /// </summary>
        public virtual Task OnFileDetectedAsync(IFileEntry file)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 阶段二：默认不参与竞价 (返回 null)
        /// Phase 2: Do not bid by default (return null)
        /// </summary>
        public virtual Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
        {
            return Task.FromResult<RouteProposal?>(null);
        }

        // --- 核心能力 / Core Capabilities ---

        /// <summary>
        /// 安全上下文：用于执行文件移动、重命名、删除等受限操作
        /// Safe Context: Used for performing restricted operations like move, rename, and delete
        /// </summary>
        protected SafeContext? Context { get; private set; }

        /// <summary>
        /// 插件专属存储：用于保存插件设置、运行记录或持久化数据
        /// Plugin Specific Storage: Used for saving settings, runtime logs, or persistent data
        /// </summary>
        protected IPluginStorage? Storage { get; private set; }

        /// <summary>
        /// 插件管理器 (系统服务)
        /// Plugin Manager (System Service)
        /// </summary>
        protected IPluginManager? PluginManager { get; private set; }

        private Func<IFileManager?>? _fileManagerFactory;

        /// <summary>
        /// 文件管理器 (系统服务) - 懒加载
        /// File Manager (System Service) - Lazy Loaded
        /// </summary>
        protected IFileManager? FileManager => _fileManagerFactory?.Invoke();

        /// <summary>
        /// 初始化方法：由主程序在加载插件时调用，注入核心能力
        /// Initialization method: Called by the main application during loading to inject capabilities
        /// </summary>
        /// <param name="context">安全上下文 / Safe Context</param>
        /// <param name="storage">专属存储 / Specific Storage</param>
        /// <param name="pluginManager">插件管理器 / Plugin Manager</param>
        /// <param name="fileManagerFactory">文件管理器工厂 / File Manager Factory</param>
        public void Initialize(SafeContext context, IPluginStorage storage, IPluginManager? pluginManager = null, Func<IFileManager?>? fileManagerFactory = null)
        {
            Context = context;
            Storage = storage;
            PluginManager = pluginManager;
            _fileManagerFactory = fileManagerFactory;
            OnInitialized();
        }

        /// <summary>
        /// 当插件初始化完成时调用 (钩子)
        /// Called when plugin initialization is complete (Hook)
        /// </summary>
        protected virtual void OnInitialized() { }

        // --- 快捷 Helper 方法 / Shortcut Helper Methods ---

        /// <summary>
        /// 快捷重命名方法
        /// Shortcut method for renaming files
        /// </summary>
        protected async Task Rename(IFileEntry file, string newName)
        {
            if (Context == null) throw new InvalidOperationException("Plugin not initialized");
            await Context.RenameAsync(file, newName);
        }

        /// <summary>
        /// 快捷移动方法
        /// Shortcut method for moving files
        /// </summary>
        protected async Task Move(IFileEntry file, string destinationFolder)
        {
            if (Context == null) throw new InvalidOperationException("Plugin not initialized");
            await Context.MoveAsync(file, destinationFolder);
        }

        /// <summary>
        /// 快捷删除方法 (移入回收站)
        /// Shortcut method for deleting files (Move to Recycle Bin)
        /// </summary>
        protected async Task Delete(IFileEntry file)
        {
            if (Context == null) throw new InvalidOperationException("Plugin not initialized");
            await Context.DeleteAsync(file);
        }

        /// <summary>
        /// 安全修改文件内容
        /// Safely modify file content with automatic backup and undo support
        /// </summary>
        /// <param name="file">The file to modify</param>
        /// <param name="modifyAction">Function that takes the file path and performs IO. Return true if successful.</param>
        protected async Task ModifyContent(IFileEntry file, Func<string, Task<bool>> modifyAction)
        {
             if (Context == null) throw new InvalidOperationException("Plugin not initialized");
             await Context.ModifyContentAsync(file, modifyAction);
        }

        /// <summary>
        /// 旧版执行接口实现 (保留兼容性)
        /// Legacy execution interface implementation (Retains compatibility)
        /// </summary>
        public virtual Task ExecuteAsync(IList<IFileEntry> files) => Task.CompletedTask;
    }
}