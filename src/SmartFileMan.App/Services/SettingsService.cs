using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SmartFileMan.Contracts.Services;

namespace SmartFileMan.App.Services
{
    public class SettingsService : ISettingsService
    {
        private const string KeyIgnoredExtensions = "IgnoredExtensions";
        private const string KeyPluginStates = "PluginStates";
        private const string KeyPluginOrder = "PluginOrder";
        private const string KeyDeveloperMode = "DeveloperMode";
        private const string KeyWatchedFolders = "WatchedFolders";

        public Task<List<string>> GetIgnoredExtensionsAsync()
        {
            string json = Preferences.Get(KeyIgnoredExtensions, "[]");
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return Task.FromResult(list);
        }

        public async Task AddIgnoredExtensionAsync(string extension)
        {
            var list = await GetIgnoredExtensionsAsync();
            if (!list.Contains(extension))
            {
                list.Add(extension);
                SaveList(KeyIgnoredExtensions, list);
            }
        }

        public async Task RemoveIgnoredExtensionAsync(string extension)
        {
            var list = await GetIgnoredExtensionsAsync();
            if (list.Remove(extension))
            {
                SaveList(KeyIgnoredExtensions, list);
            }
        }

        public bool IsPluginEnabled(string pluginId)
        {
            string json = Preferences.Get(KeyPluginStates, "{}");
            var states = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            
            // ƒ¨»œ∆Ù”√
            return states.ContainsKey(pluginId) ? states[pluginId] : true;
        }

        public Task SetPluginEnabledAsync(string pluginId, bool enabled)
        {
            string json = Preferences.Get(KeyPluginStates, "{}");
            var states = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            
            states[pluginId] = enabled;
            
            Preferences.Set(KeyPluginStates, JsonSerializer.Serialize(states));
            return Task.CompletedTask;
        }

        public List<string> GetPluginOrder()
        {
            string json = Preferences.Get(KeyPluginOrder, "[]");
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public Task SetPluginOrderAsync(List<string> pluginIds)
        {
            SaveList(KeyPluginOrder, pluginIds);
            return Task.CompletedTask;
        }

        public bool IsDeveloperModeEnabled()
        {
            return Preferences.Get(KeyDeveloperMode, false);
        }

        public Task SetDeveloperModeEnabledAsync(bool enabled)
        {
            Preferences.Set(KeyDeveloperMode, enabled);
            return Task.CompletedTask;
        }

        public Task<List<string>> GetWatchedFoldersAsync()
        {
            string json = Preferences.Get(KeyWatchedFolders, "[]");
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return Task.FromResult(list);
        }

        public async Task AddWatchedFolderAsync(string path)
        {
            var list = await GetWatchedFoldersAsync();
            if (!list.Contains(path))
            {
                list.Add(path);
                SaveList(KeyWatchedFolders, list);
            }
        }

        public async Task RemoveWatchedFolderAsync(string path)
        {
            var list = await GetWatchedFoldersAsync();
            if (list.Remove(path))
            {
                SaveList(KeyWatchedFolders, list);
            }
        }

        private void SaveList<T>(string key, List<T> list)
        {
            string json = JsonSerializer.Serialize(list);
            Preferences.Set(key, json);
        }
    }
}
