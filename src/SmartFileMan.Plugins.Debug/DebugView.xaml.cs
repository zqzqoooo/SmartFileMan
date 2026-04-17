using System.Collections.ObjectModel;
using System.Linq; // For extensions
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; 
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;
using System;
using System.IO;  

namespace SmartFileMan.Plugins.Debug
{
    public partial class DebugView : ContentView
    {
        private readonly DebugPlugin _plugin;
        public ObservableCollection<LogModel> Logs { get; } = new();
        public ObservableCollection<BiddingResultModel> BiddingResults { get; } = new();
        public ObservableCollection<MetadataItem> MetadataItems { get; } = new(); // Added
        private string _selectedFilePath;

        public DebugView(DebugPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;
            BindingContext = this; 
            LogList.ItemsSource = Logs;
            BiddingResultsList.ItemsSource = BiddingResults;
            // MetadataItems is bound in XAML directly via BindingContext = this
            
            // Subscribe to plugin events
            _plugin.LogReceived += OnLogReceived;
        }

        public void UpdateTestFileStatus(string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LblTestFile.Text = status;
                LblApiOutput.Text += $"\n[State Update] {status}";
                
                // Try to extract path from status if it's a path
                // "File Deleted" logic?
                if (status == "File Deleted" || status.StartsWith("Error")) {
                    MetadataItems.Clear();
                } 
                else if (File.Exists(status)) {
                    RefreshMetadata(status);
                }
            });
        }

        private void RefreshMetadata(string path)
        {
            try
            {
                MetadataItems.Clear();
                var info = new FileInfo(path);
                MetadataItems.Add(new MetadataItem("Name", info.Name));
                MetadataItems.Add(new MetadataItem("Extension", info.Extension));
                MetadataItems.Add(new MetadataItem("Full Path", info.FullName));
                MetadataItems.Add(new MetadataItem("Directory", info.DirectoryName ?? "Root"));
                MetadataItems.Add(new MetadataItem("Size (Bytes)", info.Length.ToString("N0")));
                MetadataItems.Add(new MetadataItem("Creation Time", info.CreationTime.ToString("g")));
                MetadataItems.Add(new MetadataItem("Last Write Time", info.LastWriteTime.ToString("g")));
                MetadataItems.Add(new MetadataItem("Last Access Time", info.LastAccessTime.ToString("g")));
                MetadataItems.Add(new MetadataItem("Attributes", info.Attributes.ToString()));
                MetadataItems.Add(new MetadataItem("Is ReadOnly", info.IsReadOnly.ToString()));
                MetadataItems.Add(new MetadataItem("Exists", info.Exists.ToString()));
            }
            catch (Exception ex)
            {
                MetadataItems.Add(new MetadataItem("Error reading metadata", ex.Message));
            }
        }

        // Tab Switching
        private void OnTabConsoleClicked(object sender, EventArgs e) => SetTab(0);
        private void OnTabSimulatorClicked(object sender, EventArgs e) => SetTab(1);
        private void OnTabApiClicked(object sender, EventArgs e) => SetTab(2);
        private void OnTabStorageClicked(object sender, EventArgs e) 
        {
            SetTab(3);
            // 切换时刷新插件列表
            // Refresh plugin list on switch
            RefreshPluginList();
        }

        private void SetTab(int index)
        {
            ViewConsole.IsVisible = index == 0;
            ViewSimulator.IsVisible = index == 1;
            ViewApiTester.IsVisible = index == 2;
            ViewStorage.IsVisible = index == 3;

            UpdateButtonState(BtnConsole, index == 0);
            UpdateButtonState(BtnSimulator, index == 1);
            UpdateButtonState(BtnApi, index == 2);
            UpdateButtonState(BtnStorage, index == 3);
        }

        private void RefreshPluginList()
        {
             _plugin.Log(LogLevel.Info, "UI", "DB", "Refreshing database collections...");
             try 
             {
                 // Get actual database collections instead of guessing plugin IDs
                 var collections = _plugin.GetDatabaseCollections().ToList();
                 _plugin.Log(LogLevel.Info, "UI", "DB", $"Found {collections.Count} collections.");
                 
                 // If empty (e.g. fresh start), maybe add file_tracker manually just in case
                 if (!collections.Contains("file_tracker"))
                 {
                     collections.Insert(0, "file_tracker");
                     _plugin.Log(LogLevel.Info, "UI", "DB", "Added default 'file_tracker'.");
                 }
                 
                 PluginPicker.ItemsSource = collections;
                 if (collections.Count > 0) PluginPicker.SelectedIndex = 0;
             }
             catch (Exception ex)
             {
                 _plugin.Log(LogLevel.Error, "UI", "DB", $"Error refreshing list: {ex.Message}");
             }
        }

        private async void OnInspectStorageClicked(object sender, EventArgs e)
        {
            try
            {
                if (PluginPicker.SelectedIndex == -1 || PluginPicker.SelectedItem == null) 
                {
                    StorageOutput.Text = "Please select a collection.";
                    _plugin.Log(LogLevel.Warning, "UI", "VAL", "No collection selected.");
                    return;
                }

                string id = (string)PluginPicker.SelectedItem;
                _plugin.Log(LogLevel.Info, "UI", "DB", $"Inspecting collection: {id}");
                
                StorageOutput.Text = "Loading storage dump...";
                
                var json = await _plugin.GetPluginStorageDumpAsync(id);
                
                if (string.IsNullOrEmpty(json)) 
                {
                     StorageOutput.Text = "(Empty Result)";
                     _plugin.Log(LogLevel.Warning, "UI", "DB", "Returned empty JSON.");
                }
                else
                {
                    StorageOutput.Text = json;
                    _plugin.Log(LogLevel.Info, "UI", "DB", $"Loaded {json.Length} chars.");
                }
            }
            catch (Exception ex)
            {
                StorageOutput.Text = $"Error: {ex.Message}";
                _plugin.Log(LogLevel.Error, "UI", "ERR", $"Inspect failed: {ex.Message}");
            }
        }

        private void UpdateButtonState(Button btn, bool isActive)
        {
            btn.TextColor = isActive ? Colors.White : Colors.Gray;
            btn.BackgroundColor = isActive ? Color.FromArgb("#333") : Colors.Transparent;
        }

        private async void OnCreateTestFileClicked(object sender, EventArgs e)
        {
            await _plugin.CreateTestFileAsync();
        }

        private async void OnTestRenameClicked(object sender, EventArgs e)
        {
            string newName = await Application.Current.MainPage.DisplayPromptAsync("Rename Test", "Enter new name:", initialValue: "renamed_test.txt");
            if (!string.IsNullOrWhiteSpace(newName))
            {
                await _plugin.TestRenameAsync(newName);
            }
        }

        private async void OnTestMoveClicked(object sender, EventArgs e)
        {
            string defaultTarget = Path.Combine(Path.GetTempPath(), "SmartFileMan_Debug", "Moved");
            Directory.CreateDirectory(defaultTarget);
            
            string target = await Application.Current.MainPage.DisplayPromptAsync("Move Test", "Enter target path:", initialValue: defaultTarget);
            if (!string.IsNullOrWhiteSpace(target))
            {
                await _plugin.TestMoveAsync(target);
            }
        }

        private async void OnTestDeleteClicked(object sender, EventArgs e)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Delete Test", "Test Safe Delete?", "Yes", "No");
            if (confirm)
            {
                await _plugin.TestDeleteAsync();
            }
        }

        private async void OnPickFileClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync();
                if (result != null)
                {
                    _selectedFilePath = result.FullPath;
                    SimFileEntry.Text = _selectedFilePath;
                    _plugin.Log(LogLevel.Info, "UI", "I100", $"Selected file: {_selectedFilePath}");
                }
            }
            catch (Exception ex)
            {
                _plugin.Log(LogLevel.Error, "UI", "E100", $"Error picking file: {ex.Message}");
            }
        }

        private async void OnProcessClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                _plugin.Log(LogLevel.Warning, "UI", "W100", "Please select a valid file first.");
                return;
            }

            var fileEntry = new SimpleFileEntry(_selectedFilePath);
            BiddingResults.Clear();

            _plugin.Log(LogLevel.Info, "SIM", "S001", "Starting Bidding Simulation...");

            try
            {
                var results = await _plugin.RunSimulationAsync(fileEntry);
                if (results == null) return;

                foreach (var res in results)
                {
                    BiddingResults.Add(new BiddingResultModel(res));
                }
                _plugin.Log(LogLevel.Success, "SIM", "S002", $"Simulation complete. {results.Count} plugins.");
            }
            catch (Exception ex)
            {
                _plugin.Log(LogLevel.Error, "SIM", "E999", ex.Message);
            }
        }

        private void OnLogReceived(LogModel log)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Logs.Add(log);
                if (Logs.Count > 100) Logs.RemoveAt(0);
            });
        }

        private void OnClearClicked(object sender, System.EventArgs e)
        {
            Logs.Clear();
        }

        public int GetBidScore()
        {
            // Simplified for now, as we removed the Entry from UI
            return 10; 
        }

        private async void OnRunSqlClicked(object sender, EventArgs e)
        {
            string sql = SqlEntry.Text;
            if (string.IsNullOrWhiteSpace(sql)) return;

            try
            {
                StorageOutput.Text = "Running Query...";
                var result = await _plugin.RunSqlAsync(sql);
                StorageOutput.Text = result;
                _plugin.Log(LogLevel.Info, "UI", "SQL", "Query executed."); // 查询已执行
            }
            catch (Exception ex)
            {
                StorageOutput.Text = $"Error: {ex.Message}";
                _plugin.Log(LogLevel.Error, "UI", "SQL", $"Query failed: {ex.Message}"); // 查询失败
            }
        }
    }

    public enum LogLevel { Info, Success, Warning, Error }

    public class LogModel
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        
        public Color DisplayColor => Level switch
        {
            LogLevel.Success => Colors.LightGreen,
            LogLevel.Warning => Colors.Orange,
            LogLevel.Error => Colors.Red,
            _ => Colors.White
        };

        public string DisplayTime => Timestamp.ToString("HH:mm:ss.fff");
    }

    // A simple implementation of IFileEntry to avoid referencing Core
    public class SimpleFileEntry : IFileEntry
    {
        private readonly FileInfo _info;
        public SimpleFileEntry(string path)
        {
            _info = new FileInfo(path);
            Id = Guid.NewGuid().ToString();
            FullPath = path;
        }

        public string Id { get; }
        public string Name => _info.Name;
        public string Extension => _info.Extension.ToLower();
        public string FullPath { get; }
        public long SizeBytes => _info.Length;
        public DateTime CreationTime => _info.CreationTime;

        public string DirectoryPath => _info.DirectoryName ?? "";
        public DateTime LastWriteTime => _info.LastWriteTime;
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        // Match generic constraint 'where T : class' and return type 'T?'
        public Task<T?> GetMetadataAsync<T>() where T : class => Task.FromResult<T?>(null);
        public Task<string> GetHashAsync() => Task.FromResult("dummy_hash");
        public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(_info.OpenRead());
    }

    public class BiddingResultModel
    {
        private readonly BiddingResult _raw;
        public BiddingResultModel(BiddingResult raw)
        {
            _raw = raw;
        }

        public string PluginName => _raw.PluginName + (_raw.IsWinner ? " 🏆" : "");
        public Color NameColor => _raw.IsWinner ? Colors.Gold : Colors.White;
        
        public string ScoreDisplay => _raw.Proposal?.Score.ToString() ?? "-";
        public Color ScoreColor => (_raw.Proposal?.Score ?? 0) > 80 ? Colors.LightGreen : Colors.Gray;

        public string DurationMs => $"{_raw.Duration.TotalMilliseconds:N0}ms";
        
        public string ProposalDetails => _raw.ErrorMessage != null 
            ? $"Error: {_raw.ErrorMessage}" 
            : (_raw.Proposal?.DestinationPath ?? "No Bid (null)");
    }

    public class MetadataItem
    {
        public string Key { get; }
        public string Value { get; }
        public MetadataItem(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
