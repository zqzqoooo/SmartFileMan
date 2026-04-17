using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.UI;
using SmartFileMan.Sdk;

namespace SmartFileMan.Plugins.Debug
{
    public class DebugPlugin : PluginBase, IFilePlugin, IPluginUI
    {
        public override string Id => "com.smartfileman.debug";
        public override string DisplayName => "🔧 Debugger";
        public override string Description => "Logs file events and tests bidding pipeline.";
        public override PluginType Type => PluginType.General;

        public event Action<LogModel> LogReceived;

        private DebugView _view;

        protected override void OnInitialized()
        {
            if (Context != null)
            {
                Context.SystemLogBroadcast += OnSystemLogBroadcast;
            }
        }

        private void OnSystemLogBroadcast(string category, string code, string message)
        {
             // Forward to our Log method which updates UI
             Log(LogLevel.Info, category, code, message);
        }

        public View GetView()
        {
            if (_view == null) _view = new DebugView(this);
            return _view;
        }

        public override Task AnalyzeBatchAsync(BatchContext context)
        {
            Log(LogLevel.Info, "BATCH", "B001", $"Phase 0: Analyzing batch of {context.AllFiles.Count} files. BatchId: {context.BatchId}");
            foreach (var file in context.AllFiles)
            {
                Log(LogLevel.Info, "BATCH", "B002", $"  - {file.Name} ({file.SizeBytes} bytes)");
            }
            return Task.CompletedTask;
        }

        public override Task OnFileDetectedAsync(IFileEntry file)
        {
            Log(LogLevel.Info, "OBS", "I001", $"Seen: {file.Name} ({file.SizeBytes} bytes)");
            return Task.CompletedTask;
        }

        public override Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
        {
            int score = _view?.GetBidScore() ?? 10;
            string targetPath = System.IO.Path.Combine(file.DirectoryPath, "Debug", "Target");
            return Task.FromResult<RouteProposal?>(new RouteProposal(targetPath, score, "Debug Plugin Bid"));
        }

        // --- API Testing Methods for UI ---

        public async Task CreateTestFileAsync()
        {
            try
            {
                // Create a test file in the AppData or a generic location
                // Just for testing the APIs.
                string fileName = $"test_file_{DateTime.Now.Ticks}.txt";
                string folder = Path.Combine(System.IO.Path.GetTempPath(), "SmartFileMan_Debug");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, fileName);
                
                await System.IO.File.WriteAllTextAsync(path, "This is a test file created by Debug Plugin.");
                
                Log(LogLevel.Info, "API", "TEST", $"Created test file: {path}");
                
                // Track this file for next operations
                _lastTestFilePath = path;
                _view?.UpdateTestFileStatus(path);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "API", "ERR", $"Failed to create file: {ex.Message}");
            }
        }

        private string? _lastTestFilePath;

        public async Task TestRenameAsync(string newName)
        {
            if (string.IsNullOrEmpty(_lastTestFilePath) || !System.IO.File.Exists(_lastTestFilePath))
            {
                Log(LogLevel.Warning, "API", "WARN", "No test file found. Create one first.");
                return;
            }

            var entry = new SimpleFileEntry(_lastTestFilePath);
            Log(LogLevel.Info, "API", "TEST", $"Testing Rename on {entry.Name} -> {newName}");
            
            try
            {
                await Rename(entry, newName);
                // Assume success if no exception (SafeContext handles UI/Errors)
                // Update tracked path (heuristically)
                string dir = Path.GetDirectoryName(_lastTestFilePath)!;
                _lastTestFilePath = Path.Combine(dir, newName);
                _view?.UpdateTestFileStatus(_lastTestFilePath);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "API", "ERR", $"Rename failed: {ex.Message}");
            }
        }

        public async Task TestMoveAsync(string targetFolder)
        {
            if (string.IsNullOrEmpty(_lastTestFilePath) || !System.IO.File.Exists(_lastTestFilePath))
            {
                Log(LogLevel.Warning, "API", "WARN", "No test file found. Create one first.");
                return;
            }

            var entry = new SimpleFileEntry(_lastTestFilePath);
            Log(LogLevel.Info, "API", "TEST", $"Testing Move on {entry.Name} -> {targetFolder}");

            try
            {
                await Move(entry, targetFolder);
                
                _lastTestFilePath = Path.Combine(targetFolder, entry.Name);
                _view?.UpdateTestFileStatus(_lastTestFilePath);
            }
            catch (Exception ex)
            {
                 Log(LogLevel.Error, "API", "ERR", $"Move failed: {ex.Message}");
            }
        }

        public async Task TestDeleteAsync()
        {
             if (string.IsNullOrEmpty(_lastTestFilePath) || !System.IO.File.Exists(_lastTestFilePath))
            {
                Log(LogLevel.Warning, "API", "WARN", "No test file found. Create one first.");
                return;
            }

            var entry = new SimpleFileEntry(_lastTestFilePath);
            Log(LogLevel.Info, "API", "TEST", $"Testing Delete (Safe Recycle) on {entry.Name}");

            try
            {
                await Delete(entry);
                _lastTestFilePath = null;
                _view?.UpdateTestFileStatus("File Deleted");
            }
            catch (Exception ex)
            {
                 Log(LogLevel.Error, "API", "ERR", $"Delete failed: {ex.Message}");
            }
        }

        // --- Storage Inspector Methods ---

        public IEnumerable<string> GetDatabaseCollections()
        {
            if (PluginManager == null) return new List<string> { "Error: No Plugin Manager" };
            return PluginManager.GetDatabaseCollections();
        }

        public async Task<string> GetPluginStorageDumpAsync(string pluginId)
        {
            if (PluginManager == null) return "Error: Plugin Manager not initialized.";
            return await PluginManager.GetPluginStorageDumpAsync(pluginId);
        }

        public async Task<string> RunSqlAsync(string sql)
        {
             if (PluginManager == null) return "Error: Plugin Manager not initialized.";
             return await PluginManager.ExecuteQueryAsync(sql);
        }

        public void Log(LogLevel level, string category, string code, string message)
        {
            var log = new LogModel
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Code = code,
                Message = message
            };

            LogReceived?.Invoke(log);
            System.Diagnostics.Debug.WriteLine($"[DebugPlugin] [{level}] {message}");
        }

        public async Task<IList<BiddingResult>> RunSimulationAsync(IFileEntry file)
        {
            if (PluginManager == null) return null;
            return await PluginManager.SimulateBiddingAsync(file);
        }
    }
}
