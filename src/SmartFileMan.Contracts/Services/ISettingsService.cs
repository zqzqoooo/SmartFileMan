using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFileMan.Contracts.Services
{
    public interface ISettingsService
    {
        // 获取忽略的文件扩展名列表 (e.g. ".tmp", ".log")
        Task<List<string>> GetIgnoredExtensionsAsync();
        Task AddIgnoredExtensionAsync(string extension);
        Task RemoveIgnoredExtensionAsync(string extension);

        // 插件启用状态
        bool IsPluginEnabled(string pluginId);
        Task SetPluginEnabledAsync(string pluginId, bool enabled);

        // 插件排序 (返回 PluginId 列表)
        List<string> GetPluginOrder();
        Task SetPluginOrderAsync(List<string> pluginIds);

        // 开发者模式
        bool IsDeveloperModeEnabled();
        Task SetDeveloperModeEnabledAsync(bool enabled);
    }
}
