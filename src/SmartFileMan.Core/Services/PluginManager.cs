using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Services;
using LiteDB;
using SmartFileMan.Sdk;
using SmartFileMan.Sdk.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 插件管理器：负责插件的加载、初始化和执行
    /// Plugin Manager: Responsible for loading, initializing, and executing plugins
    /// </summary>
    public class PluginManager : IPluginManager
    {
        // 插件列表
        // List of loaded plugins
        public List<IPlugin> Plugins { get; } = new List<IPlugin>();
        IEnumerable<IPlugin> IPluginManager.Plugins => Plugins;

        private readonly LiteDatabase _db;
        private readonly SafeContext _context;
        private readonly ISettingsService _settings;
        private readonly IServiceProvider _serviceProvider; // Inject ServiceProvider
        private readonly ILogger<PluginManager> _logger;

        private FileSystemWatcher? _pluginWatcher;
        public event EventHandler? PluginsChanged;

        // 构造函数：注入依赖并加载插件
        // Constructor: Inject dependencies and load plugins
        public PluginManager(LiteDatabase db, SafeContext context, ISettingsService settings, IServiceProvider serviceProvider, ILogger<PluginManager> logger)
        {
            _db = db;
            _context = context;
            _settings = settings;
            _serviceProvider = serviceProvider;
            _logger = logger; // Inject Logger
            
            LoadDynamicPlugins();
            InitializeWatcher();
        }

        private void InitializeWatcher()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pluginsDir = Path.Combine(baseDir, "Plugins");

            if (!Directory.Exists(pluginsDir)) Directory.CreateDirectory(pluginsDir);

            _pluginWatcher = new FileSystemWatcher(pluginsDir, "*.dll")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _pluginWatcher.Created += OnPluginFileCreated;
            // .Changed 事件可能会在复制过程中多次触发，为了简化演示坚持使用 Created
            // .Changed might trigger multiple times during copy, stick to Created or handle stable check
            // 复制大文件可能需要重试逻辑。
            // For simplicity in this demo, just Created. Copying large files might need retry logic.
        }

        private async void OnPluginFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("New plugin file detected: {Path}", e.FullPath);

            // 等待文件锁释放
            // Wait a bit for file lock release
            await Task.Delay(1000);

            try
            {
                // 尝试加载此特定插件
                // Attempt to load this specific plugin
                LoadSinglePlugin(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hot-load plugin: {Path}", e.FullPath);
            }
        }

        private readonly Dictionary<string, string> _pluginFileMap = new();

        public void DeletePlugin(IPlugin plugin)
        {
            if (_pluginFileMap.TryGetValue(plugin.Id, out string? path))
            {
                if (File.Exists(path))
                {
                    try 
                    {
                        File.Delete(path);
                        _logger.LogInformation("Deleted plugin file: {Path}", path);
                        // The FileSystemWatcher will trigger OnPluginDeleted? 
                        // Wait, FileSystemWatcher might be tricky with deletes if file is locked by assembly load context.
                        // .NET Core AssemblyLoadContext doesn't lock files usually? 
                        // Actually, default assembly loading DOES lock files on Windows.
                        // We need to ensure we loaded with memory stream or shadow copy if we want to delete.
                        // DllPluginLoader logic?
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete plugin file: {Path}", path);
                        throw new Exception($"Cannot delete plugin file (System might lock it): {ex.Message}");                    }
                }
            }
        }

        private void LoadSinglePlugin(string dllPath)
        {
             bool isDevMode = _settings.IsDeveloperModeEnabled();
             var loader = new DllPluginLoader();
             
             var plugin = loader.LoadPluginFromFile(dllPath, isDevMode);
             if (plugin != null)
             {
                 // Check for duplicates
                 if (Plugins.Any(p => p.Id == plugin.Id))
                 {
                     _logger.LogWarning("Plugin {Id} already loaded. Skipping hot-load.", plugin.Id);
                     return;
                 }

                 InitializePlugin(plugin);
                 Plugins.Add(plugin);
                 _pluginFileMap[plugin.Id] = dllPath; // Track path
                 
                 _logger.LogInformation("Hot-loaded plugin: {Name}", plugin.DisplayName);
                 PluginsChanged?.Invoke(this, EventArgs.Empty);
             }
        }

        private void InitializePlugin(IPlugin plugin)
        {
            // 从设置中读取启用状态
            plugin.IsEnabled = _settings.IsPluginEnabled(plugin.Id);

            if (plugin is PluginBase pb)
            {
                var storage = new LiteDbStorage(_db, pb.Id);
                // Inject Context and Storage services
                pb.Initialize(_context, storage, this, () => _serviceProvider.GetService<IFileManager>());
            }
        }

        /// <summary>
        /// 从指定目录动态加载并初始化插件
        /// Dynamically load and initialize plugins from the specified directory
        /// </summary>
        private void LoadDynamicPlugins()
        {
            _logger.LogInformation("Starting to load dynamic plugins...");
            var loader = new DllPluginLoader();

            // Determine plugin directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pluginsDir = Path.Combine(baseDir, "Plugins");

            // Ensure directory exists
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            try
            {
                // 检查开发者模式
                // Check developer mode
                bool isDevMode = _settings.IsDeveloperModeEnabled();
                if (isDevMode)
                {
                    _logger.LogWarning("Developer Mode enabled. Skipping signature verification.");
                    System.Diagnostics.Debug.WriteLine("[警告] 开发者模式已启用，将跳过插件签名验证。");
                }

                // 扫描并加载 DLL
                // Scan and load DLLs
                var loadedPlugins = loader.LoadPluginsFromFolder(pluginsDir, isDevMode);
                _logger.LogInformation("Loaded {Count} plugins from {Dir}", loadedPlugins.Count(), pluginsDir);

                // 添加到内存列表
                // Add to in-memory list
                 foreach (var p in loadedPlugins)
                 {
                     if (!Plugins.Any(existing => existing.Id == p.Id))
                     {
                         Plugins.Add(p);
                         try {
                            if (!string.IsNullOrEmpty(p.GetType().Assembly.Location))
                                _pluginFileMap[p.Id] = p.GetType().Assembly.Location;
                         } catch {}
                     }
                 }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during plugin loading");
                System.Diagnostics.Debug.WriteLine($"[严重错误] 插件加载流程异常: {ex.Message}");
            }

            // Loop to initialize plugins
            foreach (var plugin in Plugins)
            {
                InitializePlugin(plugin);
            }
        }

        public async Task AnalyzeBatchAsync(BatchContext context)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin is IFilePlugin filePlugin && plugin.IsEnabled)
                {
                    try
                    {
                        await filePlugin.AnalyzeBatchAsync(context);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "Error in AnalyzeBatchAsync for plugin {Name}", plugin.DisplayName);
                    }
                }
            }
        }

        public async Task<IList<BiddingResult>> SimulateBiddingAsync(IFileEntry file)
        {
            var results = new List<BiddingResult>();
            var sw = new System.Diagnostics.Stopwatch();

            // Call batch analysis before bidding to simulate Phase 0 context
            var mockContext = new BatchContext("SimulationBatch", new List<IFileEntry> { file });
            await AnalyzeBatchAsync(mockContext);

            foreach (var plugin in Plugins)
            {
                if (!plugin.IsEnabled) continue;

                var result = new BiddingResult 
                { 
                    PluginName = plugin.DisplayName, 
                    PluginType = (plugin is PluginBase pb) ? pb.Type.ToString() : "Unknown"
                };

                if (plugin is IFilePlugin filePlugin)
                {
                    sw.Restart();
                    try
                    {
                        await filePlugin.OnFileDetectedAsync(file);
                        result.Proposal = await filePlugin.ProposeDestinationAsync(file);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = ex.Message;
                        _logger.LogError(ex, "Simulation error in {Plugin}", plugin.DisplayName);
                    }
                    sw.Stop();
                    result.Duration = sw.Elapsed;
                }
                else
                {
                    result.ErrorMessage = "Not an IFilePlugin";
                }

                results.Add(result);
            }

            // Mark winner logic
            var winner = results
                .Where(r => r.Proposal != null)
                .OrderByDescending(r => r.Proposal.Score)
                .FirstOrDefault();

            if (winner != null) winner.IsWinner = true;

            return results;
        }

        /// <summary>
        /// Bidding Arbitration
        /// </summary>
        public async Task<RouteProposal?> GetBestRouteAsync(IFileEntry file)
        {
            var proposals = new List<RouteProposal>();

            var orderList = _settings.GetPluginOrder();

            var sortedPlugins = Plugins.OrderBy(p => 
            {
                int index = orderList.IndexOf(p.Id);
                return index == -1 ? int.MaxValue : index;
            }).ToList();

            var detectionTasks = sortedPlugins
                .Where(p => p is IFilePlugin fp && fp.IsEnabled)
                .Select(async p =>
                {
                    try
                    {
                        await ((IFilePlugin)p).OnFileDetectedAsync(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in OnFileDetectedAsync for plugin {PluginName}", p.DisplayName);
                    }
                    return p;
                })
                .ToList();

            await Task.WhenAll(detectionTasks);
            _logger.LogDebug("Phase 1 (Detection) completed for {Count} plugins", detectionTasks.Count);

            var biddingTasks = sortedPlugins
                .Where(p => p is IFilePlugin fp && fp.IsEnabled)
                .Select(async p =>
                {
                    try
                    {
                        var proposal = await ((IFilePlugin)p).ProposeDestinationAsync(file);
                        return new { Plugin = p, Proposal = proposal };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ProposeDestinationAsync for plugin {PluginName}", p.DisplayName);
                        return new { Plugin = p, Proposal = (RouteProposal?)null };
                    }
                })
                .ToList();

            var results = await Task.WhenAll(biddingTasks);
            _logger.LogDebug("Phase 2 (Bidding) completed for {Count} plugins", biddingTasks.Count);

            // Collect all proposals
            foreach (var result in results)
            {
                if (result.Proposal != null)
                {
                    result.Proposal.PluginName = result.Plugin.DisplayName;

                    // Log bidding
                    _logger.LogInformation("Bid received: {PluginName} -> Score: {Score}, Path: {Path}, Explanation: {Exp}",
                        result.Plugin.DisplayName, result.Proposal.Score, result.Proposal.DestinationPath, result.Proposal.Explanation);

                    proposals.Add(result.Proposal);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PluginManager] Bid rejected or zero by {result.Plugin.DisplayName} for {file.Name}");
                    _logger.LogInformation("No Bid or rejected by: {PluginName}", result.Plugin.DisplayName);
                }
            }

            var bestProposal = proposals
                .OrderByDescending(p => p.Score)
                .FirstOrDefault();

            return bestProposal;
        }

        public Task<string> GetPluginStorageDumpAsync(string collectionName)
        {
            return Task.Run(() =>
            {
                try
                {
                    // LiteDatabase in v5 doesn't expose ConnectionString directly?
                    // Try to get info manually if possible, or just skip path logging.
                    // Actually it handles multiple engines.
                    // Let's just log names.
                    _logger.LogInformation("Dump request for: {Name}.", collectionName);
                    
                    // Direct name strategy first
                    string targetName = collectionName;
                    
                    // Check if collection exists exactly as requested
                    bool exists = _db.CollectionExists(targetName);
                    
                    if (!exists) 
                    {
                        // Check sanitized version (legacy plugin ID support)
                         string sanitized = targetName.Replace(".", "_").Replace("-", "_");
                         if (_db.CollectionExists(sanitized))
                         {
                             targetName = sanitized;
                             exists = true;
                         }
                    }

                    if (!exists)
                    {
                         // Last ditch: List all collections to log
                         var allCols = string.Join(", ", _db.GetCollectionNames());
                         _logger.LogWarning("Collection {Name} not found. Available: {All}", targetName, allCols);
                         return "[]"; // Or error message?
                    }
                    
                    var col = _db.GetCollection(targetName);
                    var count = col.Count();
                    _logger.LogInformation("Collection {Name} found with {Count} docs.", targetName, count);

                    var allDocs = col.FindAll();
                    
                    // Format JSON for readability
                    var json = LiteDB.JsonSerializer.Serialize(new BsonArray(allDocs));
                    
                    try 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    catch
                    {
                        return json; 
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dump storage for {Name}", collectionName);
                    return $"{{\"error\": \"{ex.Message}\"}}";
                }
            });
        }

        public Task<string> ExecuteQueryAsync(string sql)
        {
             return Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Executing SQL: {Sql}", sql);
                    using var reader = _db.Execute(sql);
                    
                    var results = new BsonArray();
                    while (reader.Read())
                    {
                        results.Add(reader.Current);
                    }
                    
                    var json = LiteDB.JsonSerializer.Serialize(results);
                    
                    // 尝试格式化 JSON 以提高可读性
                    // Try to format JSON for readability
                    try 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    catch
                    {
                        return json; 
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SQL Error");
                    return $"{{\"error\": \"{ex.Message}\"}}";
                }
            });
        }

        public IEnumerable<string> GetDatabaseCollections()
        {
            try
            {
                return _db.GetCollectionNames().OrderBy(x => x).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get collection names");
                return new List<string>();
            }
        }

        public Task ClearAllDataAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _logger.LogWarning("Clearing all database collections...");
                    var collections = _db.GetCollectionNames().ToList();
                    foreach (var colName in collections)
                    {
                        _logger.LogInformation("Dropping collection: {Name}", colName);
                        _db.DropCollection(colName);
                    }
                    
                    // 强制清理检查点，减小文件体积
                    // Force checkpoint to shrink file size
                    _db.Checkpoint();
                    _logger.LogInformation("All database collections cleared successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear all data");
                    throw;
                }
            });
        }
    }
}