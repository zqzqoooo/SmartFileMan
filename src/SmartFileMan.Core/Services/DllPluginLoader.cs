using SmartFileMan.Contracts.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartFileMan.Core.Services
{
    public class DllPluginLoader
    {
        private readonly PluginVerifier _verifier = new PluginVerifier();

        /// <summary>
        /// Load all plugins from the specified folder
        /// </summary>
        /// <param name="folderPath">插件文件夹路径 / Plugin folder path</param>
        /// <param name="bypassSignatureCheck">是否跳过签名检查 (开发者模式) / Whether to bypass signature check (Developer Mode)</param>
        public IEnumerable<IPlugin> LoadPluginsFromFolder(string folderPath, bool bypassSignatureCheck = false)
        {
            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
            {
                // If it doesn't exist, try to create an empty one to prevent errors
                try { Directory.CreateDirectory(folderPath); } catch { }
                yield break;
            }

            // Scan for DLL files in all subdirectories (each plugin can be in its own subdirectory)
            var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);

            foreach (var dllPath in dllFiles)
            {
                // Skip non-plugin dependencies
                var fileName = Path.GetFileName(dllPath);
                if (fileName.Equals("TagLibSharp.dll", StringComparison.OrdinalIgnoreCase)) continue;

                IPlugin? plugin = null;
                try
                {
                    // Skip system files that are not plugins
                    if (Path.GetFileName(dllPath).StartsWith("System.") ||
                        Path.GetFileName(dllPath).StartsWith("Microsoft."))
                        continue;

                    // Security Check: Verify signature
                    if (!bypassSignatureCheck && !_verifier.VerifyPlugin(dllPath))
                    {
                        continue;
                    }

                    plugin = LoadPlugin(dllPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load plugin {dllPath}: {ex.Message}");
                    Console.WriteLine($"[Error] Failed to load plugin {dllPath}: {ex}");
                }

                if (plugin != null)
                {
                    yield return plugin;
                }
            }
        }

        /// <summary>
        /// 尝试加载单个插件文件
        /// Attempt to load a single plugin file
        /// </summary>
        public IPlugin? LoadPluginFromFile(string dllPath, bool bypassSignatureCheck = false)
        {
            if (!File.Exists(dllPath)) return null;

             // 忽略系统文件
             // Ignored system files
            string fileName = Path.GetFileName(dllPath);
            if (fileName.StartsWith("System.") || fileName.StartsWith("Microsoft.")) return null;

            // 安全检查
            // Security Check
            if (!bypassSignatureCheck && !_verifier.VerifyPlugin(dllPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Security] Signature verification failed for {fileName}");
                return null;
            }

            try
            {
                return LoadPlugin(dllPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load {fileName}: {ex.Message}");
                return null;
            }
        }

        private IPlugin? LoadPlugin(string pluginPath)
        {
            // Create a separate context
            var loadContext = new PluginLoadContext(pluginPath);

            // Load assembly (Read DLL)
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

            // Find classes that implement the IPlugin interface
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    // Create instance
                    // Activator.CreateInstance is equivalent to new Class()
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