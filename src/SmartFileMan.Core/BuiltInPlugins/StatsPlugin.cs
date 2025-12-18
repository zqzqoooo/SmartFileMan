using SmartFileMan.Contracts.Models;
using SmartFileMan.Sdk;

namespace SmartFileMan.Core.BuiltInPlugins
{
    public class StatsPlugin : PluginBase
    {
        public override string Id => "core.stats";
        public override string DisplayName => "📊 内置统计器";
        public override string Description => "统计文件总数和总大小 (测试内置插件)";

        public override async Task ExecuteAsync(IList<IFileEntry> files)
        {
            // 简单的 LINQ 统计
            var totalSize = files.Sum(f => f.SizeBytes);
            var count = files.Count;

            // 格式化大小
            string sizeStr = FormatSize(totalSize);

            // 调用 SDK 的 UI 能力 (这里我们利用 Toast 显示结果)
            // 注意：因为我们继承了 PluginBase，所以可以直接用 _context (需要把 PluginBase 的 Context 属性改为 protected)
            // 让我们假设你在 PluginBase 里暴露了 Context 或者提供了 Helper 方法
            // 暂时我们假设 PluginBase 里有个 ShowMessage 方法，或者我们直接访问 Context

            // 为了演示方便，这里模拟一个业务逻辑：记录日志到控制台
            Console.WriteLine($"[StatsPlugin] 扫描完成：{count} 个文件，共 {sizeStr}");

            // 这里的 Context 是我们在 SDK PluginBase 里定义的 protected 属性
            // 这是一个很棒的测试：测试插件能否呼叫 UI
            // 假设我们在 SDK 的 PluginBase 里加一个 Helper: ShowToast
            // 如果没有，直接用 base.Context (前提是 protected)

            // *修正*: 上一步 PluginBase 代码里 Context 是 protected 的，所以可以直接用
            // 但 Context 是接口 ISafeContext 还是具体的 SafeContext? 
            // 应该是 SDK 里的 SafeContext (包含了 UI 交互)

            // 我们在 PluginBase 里没有暴露直接的 Toast，这里我们用 Context 的底层 UI 接口
            // 但 SafeContext 目前主要暴露的是 Move/Rename。
            // *补救措施*: 我们通常会在 PluginBase 加一个 ShowMessage。
            // 既然现在没有，我们就先只打 Console Log，稍后在外部插件里演示 Move。
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}