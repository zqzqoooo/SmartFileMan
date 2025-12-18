using System.Reflection;
using System.Runtime.Loader;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 插件的独立加载上下文 (相当于给每个插件一个独立的气泡)
    /// 防止插件之间的依赖冲突
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true) // 允许卸载
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // 1. 尝试从插件目录解析依赖
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // 2. 如果插件目录没有，就不用管了，系统会自动去主程序目录找 (比如 System.Private.CoreLib)
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            return IntPtr.Zero;
        }
    }
}