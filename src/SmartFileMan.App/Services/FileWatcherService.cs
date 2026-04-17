using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Core.Models;
using SmartFileMan.Core.Services;
using SmartFileMan.Sdk.Services; // SafeContext

namespace SmartFileMan.App.Services
{
    /// <summary>
    /// 文件监控服务：监听指定目录的变动并触发自动整理
    /// File Watcher Service: Monitors specified directories and triggers auto-organization
    /// </summary>
    public class FileWatcherService : IDisposable
    {
        private readonly ISettingsService _settings;
        private readonly FileManager _fileManager;
        private readonly SafeContext _safeContext; // Inject SafeContext
        private readonly ILogger<FileWatcherService> _logger; // Logger

        // 存储路径 -> Watcher 的映射
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        // 简单的防抖队列 (Debounce)
        private readonly ConcurrentDictionary<string, DateTime> _pendingFiles = new();

        private System.Timers.Timer _debounceTimer;

        public FileWatcherService(ISettingsService settings, FileManager fileManager, SafeContext safeContext, ILogger<FileWatcherService> logger)
        {
            _settings = settings;
            _fileManager = fileManager;
            _safeContext = safeContext;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var folders = await _settings.GetWatchedFoldersAsync();
            foreach (var folder in folders)
            {
                StartWatching(folder);
            }

            _debounceTimer = new System.Timers.Timer(1000); // 1 秒缓冲区 / 1 second buffer
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnDebounceElapsed;

            // 在后台触发初始扫描
            // Trigger initial scan in background
            _ = Task.Run(PerformInitialScanAsync);
        }

        public async Task PerformInitialScanAsync()
        {
            var folders = await _settings.GetWatchedFoldersAsync();
            var allFiles = new List<LocalFileEntry>();

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                try
                {
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        allFiles.Add(new LocalFileEntry(f));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during initial scan of {Folder}", folder);
                }
            }

            if (allFiles.Count > 0)
            {
                _logger.LogInformation("Initial Scan: Found {Count} files. Processing...", allFiles.Count);
                await _fileManager.ProcessBatchAsync(allFiles);
            }
        }

        public void StartWatching(string path)
        {
            if (_watchers.ContainsKey(path)) return;
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Cannot watch non-existent path: {Path}", path);
                return;
            }

            try
            {
                var watcher = new FileSystemWatcher(path);
                // 监听文件创建和重命名
                // Monitor file creation and renaming
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
                watcher.Created += OnFileCreated;
                watcher.Renamed += OnFileCreated; // 重命名进来的文件也当作新文件处理 / Renamed incoming files are treated as new files
                
                watcher.EnableRaisingEvents = true;
                _watchers[path] = watcher;
                
                _logger.LogInformation("Started watching folder: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error watching folder: {Path}", path);
            }
        }

        public void StopWatching(string path)
        {
            if (_watchers.TryGetValue(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(path);
                _logger.LogInformation("Stopped watching folder: {Path}", path);
            }
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            string fullPath = e.FullPath;
            
            // Log that we saw something (Debug level to avoid noise)
            string detectedLog = $"Scan a new file {fullPath}";
            _logger.LogInformation(detectedLog);
            _safeContext.BroadcastLog("SCAN", "NEW", detectedLog);

            if (_pendingFiles.TryAdd(fullPath, DateTime.Now))
            {                
                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
        }

        private async void OnDebounceElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Collect all pending files
            var pendingFilesSnapshot = _pendingFiles.Keys.ToList();
            _pendingFiles.Clear();
            
            if (pendingFilesSnapshot.Count == 0) return;

            var entries = new List<Core.Models.LocalFileEntry>();
            foreach (var path in pendingFilesSnapshot)
            {
                // Flattener Logic: Handle Directories
                if (Directory.Exists(path))
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            entries.Add(new LocalFileEntry(f));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Watcher] Error flattening directory {path}: {ex.Message}");
                    }
                }
                else if (File.Exists(path))
                {
                    try {
                        entries.Add(new LocalFileEntry(path));
                    } catch {}
                }
            }
            
            if (entries.Count > 0)
            {
                try
                {
                    _logger.LogInformation("Batch Debounce Triggered. Processing {Count} files found in watched folders.", entries.Count);
                    // Optional: Log individual files if debug
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        foreach (var entry in entries) _logger.LogDebug(" -> {Path}", entry.FullPath);
                    }
                    
                    // Call FileManager to process
                    var result = await _fileManager.ProcessBatchAsync(entries);
                    if (result.IsSuccess)
                        _logger.LogInformation("Batch processing completed successfully.");
                    else
                        _logger.LogWarning("Batch processing completed with issues: {Msg}", result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch from watcher.");
                }
            }
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
        }
    }
}
