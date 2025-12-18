using SmartFileMan.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartFileMan.Core.Services
{
    public class DllPluginLoader
    {
        /// <summary>
        /// 从指定文件夹加载所有插件
        /// </summary>
        /// <param name="folderPath">插件文件夹路径</param>
        public IEnumerable<IPlugin> LoadPluginsFromFolder(string folderPath)
        {
            // 确保文件夹存在
            if (!Directory.Exists(folderPath))
            {
                // 如果不存在，尝试创建一个空的，防止报错
                try { Directory.CreateDirectory(folderPath); } catch { }
                yield break;
            }

            // 1. 扫描所有 DLL 文件
            var dllFiles = Directory.GetFiles(folderPath, "*.dll");

            foreach (var dllPath in dllFiles)
            {
                // 跳过并非插件的系统文件 (比如依赖库)
                if (Path.GetFileName(dllPath).StartsWith("System.") ||
                    Path.GetFileName(dllPath).StartsWith("Microsoft."))
                    continue;

                IPlugin? plugin = null;

                try
                {
                    plugin = LoadPlugin(dllPath);
                }
                catch (Exception ex)
                {
                    // 这里可以记录日志：Plugin load failed: ex.Message
                    System.Diagnostics.Debug.WriteLine($"加载插件失败 {dllPath}: {ex.Message}");
                }

                if (plugin != null)
                {
                    yield return plugin;
                }
            }
        }

        private IPlugin? LoadPlugin(string pluginPath)
        {
            // 1. 创建独立上下文
            var loadContext = new PluginLoadContext(pluginPath);

            // 2. 加载程序集 (读取 DLL)
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

            // 3. 寻找实现了 IPlugin 接口的类
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    // 4. 创建实例
                    // Activator.CreateInstance 相当于 new Class()
                    if (Activator.CreateInstance(type) is IPlugin pluginInstance)
                    {
                        return pluginInstance;
                    }
                }
            }

            return null;
        }
    }
}