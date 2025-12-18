using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using LiteDB;
using SmartFileMan.Sdk;
using SmartFileMan.Sdk.Services;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 插件管理器：负责插件的加载、初始化和执行
    /// Plugin Manager: Responsible for loading, initializing, and executing plugins
    /// </summary>
    public class PluginManager
    {
        // 插件列表
        // List of loaded plugins
        public List<IPlugin> Plugins { get; } = new List<IPlugin>();

        private readonly LiteDatabase _db;
        private readonly SafeContext _context;

        // 构造函数：注入依赖并加载插件
        // Constructor: Inject dependencies and load plugins
        public PluginManager(LiteDatabase db, SafeContext context)
        {
            _db = db;
            _context = context;
            LoadDynamicPlugins();
        }

        /// <summary>
        /// 从指定目录动态加载并初始化插件
        /// Dynamically load and initialize plugins from the specified directory
        /// </summary>
        private void LoadDynamicPlugins()
        {
            var loader = new DllPluginLoader();

            // 确定插件目录
            // Determine the plugins directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pluginsDir = Path.Combine(baseDir, "Plugins");

            // 确保目录存在
            // Ensure the directory exists
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            // 扫描并加载 DLL
            // Scan and load DLLs
            var loadedPlugins = loader.LoadPluginsFromFolder(pluginsDir);

            // 添加到内存列表
            // Add to the in-memory list
            Plugins.AddRange(loadedPlugins);

            // 循环初始化插件
            // Loop to initialize each plugin
            foreach (var plugin in loadedPlugins)
            {
                if (plugin is PluginBase pb)
                {
                    // 创建插件专属的 LiteDB 存储实例
                    // Create a plugin-specific LiteDB storage instance
                    var storage = new LiteDbStorage(_db, pb.Id);

                    // 注入 Context 上下文和 Storage 存储服务
                    // Inject Context and Storage services
                    pb.Initialize(_context, storage);
                }
            }
        }

        /// <summary>
        /// 异步运行所有已启用的整理插件
        /// Run all enabled organizer plugins asynchronously
        /// </summary>
        /// <param name="files">待处理的文件列表 / List of files to be processed</param>
        public async Task RunAllPluginsAsync(IList<IFileEntry> files)
        {
            foreach (var plugin in Plugins)
            {
                // 检查插件是否属于整理类型且当前已启用
                // Check if the plugin is an organizer and currently enabled
                if (plugin is IOrganizerPlugin organizer && plugin.IsEnabled)
                {
                    try
                    {
                        // 执行插件逻辑
                        // Execute plugin logic
                        await organizer.ExecuteAsync(files);
                    }
                    catch (Exception ex)
                    {
                        // 捕获异常：防止单个插件崩溃导致主程序终止
                        // Catch exception: Prevent a single plugin failure from crashing the entire application
                        System.Diagnostics.Debug.WriteLine($"插件 {plugin.DisplayName} 执行出错: {ex.Message}");
                    }
                }
            }
        }
    }
}