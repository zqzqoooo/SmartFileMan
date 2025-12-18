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
    public class PluginManager
    {
        // 插件列表
        public List<IPlugin> Plugins { get; } = new List<IPlugin>();
        
        private readonly LiteDatabase _db;
        private readonly SafeContext _context;

        // 注入依赖
        public PluginManager(LiteDatabase db, SafeContext context)
        {
            _db = db;
            _context = context;
            LoadDynamicPlugins();
        }

        private void LoadDynamicPlugins()
        {
            var loader = new DllPluginLoader();

            // 1. 确定插件目录 (App.exe 旁边的 Plugins 文件夹)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pluginsDir = Path.Combine(baseDir, "Plugins");
            
            // 确保目录存在
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            // 2. 扫描并加载
            var loadedPlugins = loader.LoadPluginsFromFolder(pluginsDir);

            // 3. 添加到内存
            Plugins.AddRange(loadedPlugins);
            
            // 4. 初始化插件 (注入能力)
            foreach (var plugin in loadedPlugins)
            {
                if (plugin is PluginBase pb)
                {
                    // 创建插件专属存储
                    var storage = new LiteDbStorage(_db, pb.Id);
                    // 注入 Context 和 Storage
                    pb.Initialize(_context, storage);
                }
            }
        }

        // --- 【核心修复】必须包含这个方法，MainPage 才能调用 ---
        public async Task RunAllPluginsAsync(IList<IFileEntry> files)
        {
            foreach (var plugin in Plugins)
            {
                // 只运行启用的、且实现了 IOrganizerPlugin 接口的整理类插件
                if (plugin is IOrganizerPlugin organizer && plugin.IsEnabled)
                {
                    try
                    {
                        await organizer.ExecuteAsync(files);
                    }
                    catch (Exception ex)
                    {
                        // 防止单个插件报错导致整个程序崩溃
                        System.Diagnostics.Debug.WriteLine($"插件 {plugin.DisplayName} 执行出错: {ex.Message}");
                    }
                }
            }
        }
    }
}