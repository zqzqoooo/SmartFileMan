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
    /// 插件的基类
    /// 提供了 SafeContext (安全上下文) 和 Storage (存储) 的自动管理
    /// </summary>
    public abstract class PluginBase : IOrganizerPlugin
    {
        // 插件必须实现的基础信息
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual string Version => "1.0.0"; 
        public virtual bool IsEnabled { get; set; } = true;

        // --- 核心能力 ---

        /// <summary>
        /// 安全上下文 (用于执行移动、删除等操作)
        /// </summary>
        protected SafeContext? Context { get; private set; }

        /// <summary>
        /// 插件专属存储 (用于保存设置、记录等)
        /// </summary>
        protected IPluginStorage? Storage { get; private set; }

        /// <summary>
        /// 初始化方法 (由主程序调用，注入能力)
        /// </summary>
        /// <param name="context">安全上下文</param>
        /// <param name="storage">专属存储</param>
        public void Initialize(SafeContext context, IPluginStorage storage)
        {
            Context = context;
            Storage = storage;
        }
        
        // --- 快捷 Helper 方法 ---

        protected async Task Rename(IFileEntry file, string newName)
        {
            if (Context == null) throw new InvalidOperationException("Plugin not initialized");
            await Context.RenameAsync(file, newName);
        }

        protected async Task Move(IFileEntry file, string destinationFolder)
        {
             if (Context == null) throw new InvalidOperationException("Plugin not initialized");
             await Context.MoveAsync(file, destinationFolder);
        }

        // 抽象的执行方法，留给子类实现
        public abstract Task ExecuteAsync(IList<IFileEntry> files);
    }
}