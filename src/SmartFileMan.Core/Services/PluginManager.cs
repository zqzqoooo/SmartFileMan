using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using LiteDB;
using SmartFileMan.Sdk;
using SmartFileMan.Sdk.Services;
using SmartFileMan.Contracts.Services;

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
        private readonly ISettingsService _settings; // 新增依赖

        // 构造函数：注入依赖并加载插件
        // Constructor: Inject dependencies and load plugins
        public PluginManager(LiteDatabase db, SafeContext context, ISettingsService settings)
        {
            _db = db;
            _context = context;
            _settings = settings;
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

            try
            {
                // 检查开发者模式
                bool isDevMode = _settings.IsDeveloperModeEnabled();
                if (isDevMode)
                {
                    System.Diagnostics.Debug.WriteLine("[警告] 开发者模式已启用，将跳过插件签名验证。");
                }

                // 扫描并加载 DLL
                // Scan and load DLLs
                var loadedPlugins = loader.LoadPluginsFromFolder(pluginsDir, isDevMode);

                // 添加到内存列表
                // Add to the in-memory list
                Plugins.AddRange(loadedPlugins);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[严重错误] 插件加载流程异常: {ex.Message}");
            }

            // 循环初始化插件
            // Loop to initialize each plugin
            foreach (var plugin in Plugins)
            {
                // 从设置中读取启用状态
                plugin.IsEnabled = _settings.IsPluginEnabled(plugin.Id);

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
            // 旧逻辑保留，用于兼容
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

        /// <summary>
        /// 竞价仲裁：为文件寻找最佳归档路径
        /// Bidding Arbitration: Find the best archiving route for the file
        /// </summary>
        public async Task<RouteProposal?> GetBestRouteAsync(IFileEntry file)
        {
            var proposals = new List<RouteProposal>();

            // 获取排序设置
            var orderList = _settings.GetPluginOrder();
            
            // 对插件列表进行排序：如果在 orderList 中，按索引排；否则排在后面
            var sortedPlugins = Plugins.OrderBy(p => 
            {
                int index = orderList.IndexOf(p.Id);
                return index == -1 ? int.MaxValue : index;
            }).ToList();

            // 1. 询问所有启用的 IFilePlugin
            foreach (var plugin in sortedPlugins)
            {
                if (plugin is IFilePlugin filePlugin && plugin.IsEnabled)
                {
                    try
                    {
                        // 阶段一：通知插件 (观察)
                        await filePlugin.OnFileDetectedAsync(file);

                        // 阶段二：获取报价
                        var proposal = await filePlugin.ProposeDestinationAsync(file);
                        if (proposal != null)
                        {
                            proposals.Add(proposal);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"插件 {plugin.DisplayName} 竞价出错: {ex.Message}");
                    }
                }
            }

            // 2. 仲裁逻辑：优先 Specific，然后按分数降序
            // Arbitration Logic: Prioritize Specific, then by Score descending
            // 这里我们假设 Specific 的权重非常高，或者直接在排序时体现
            // 也可以简单地：先按 Type (Specific > General), 再按 Score
            
            // 注意：PluginType.Specific = 1, General = 0. 所以 OrderByDescending(Type) 会把 Specific 排前面
            var bestProposal = proposals
                .OrderByDescending(p => p.Score) // 先看分数 (假设分数已经包含了类型的权重，或者我们分开排)
                // 修正策略：如果 Specific 插件出价了，通常应该优先于 General，除非 General 分数极高？
                // 简单起见，我们让插件自己控制分数。Specific 插件应该给出更高的分数 (e.g. > 80)。
                // 或者我们强制排序：
                // .OrderByDescending(p => (plugin as IFilePlugin).Type) // 这很难拿到 plugin 实例，除非 proposal 包含 plugin 引用
                
                // 让我们相信分数。
                .FirstOrDefault();

            return bestProposal;
        }
    }
}