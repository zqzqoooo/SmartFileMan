using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Services;
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
    public abstract class PluginBase : IOrganizerPlugin
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
        /// 初始化方法：由主程序在加载插件时调用，注入核心能力
        /// Initialization method: Called by the main application during loading to inject capabilities
        /// </summary>
        /// <param name="context">安全上下文 / Safe Context</param>
        /// <param name="storage">专属存储 / Specific Storage</param>
        public void Initialize(SafeContext context, IPluginStorage storage)
        {
            Context = context;
            Storage = storage;
        }

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
        /// 核心执行逻辑：留给具体的插件子类实现
        /// Core execution logic: To be implemented by specific plugin subclasses
        /// </summary>
        /// <param name="files">待处理的文件列表 / List of files to be processed</param>
        public abstract Task ExecuteAsync(IList<IFileEntry> files);
    }
}