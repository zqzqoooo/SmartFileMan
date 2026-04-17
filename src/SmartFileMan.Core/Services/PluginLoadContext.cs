using System.Reflection;
using System.Runtime.Loader;
using System.IO;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 插件的独立加载上下文 (相当于给每个插件一个独立的气泡)
    /// Independent loading context for plugins (acts like an isolated bubble for each plugin)
    /// 防止插件之间的依赖冲突
    /// Prevents dependency conflicts between plugins
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly string _pluginDirectory;

        public PluginLoadContext(string pluginPath) : base(isCollectible: false) // Disable unloading to prevent context from being collected/unloaded during async operations / 禁用卸载以防止上下文在异步操作期间被回收/卸载
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
            _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (!string.IsNullOrEmpty(_pluginDirectory) && assemblyName.Name != null)
            {
                string localPath = Path.Combine(_pluginDirectory, assemblyName.Name + ".dll");
                if (File.Exists(localPath))
                {
                    try 
                    {
                        using var fs = File.OpenRead(localPath);
                        return LoadFromStream(fs);
                    }
                    catch (Exception ex)
                    {
                         try 
                         {
                             return LoadFromAssemblyPath(localPath);
                         }
                         catch { /* ignore, let next step try / 忽略，让下一步尝试 */ }
                    }
                }
            }

            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            
            // Unmanaged fallback
            if (!string.IsNullOrEmpty(_pluginDirectory))
            {
                string manualPath = Path.Combine(_pluginDirectory, unmanagedDllName);
                if (File.Exists(manualPath)) return LoadUnmanagedDllFromPath(manualPath);
                
                // Also try with .dll extension if on Windows and not provided
                if (!manualPath.EndsWith(".dll") && File.Exists(manualPath + ".dll"))
                    return LoadUnmanagedDllFromPath(manualPath + ".dll");
            }
            
            return IntPtr.Zero;
        }
    }
}