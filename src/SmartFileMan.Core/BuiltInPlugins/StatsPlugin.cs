using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Sdk;

namespace SmartFileMan.Core.BuiltInPlugins
{
    public class StatsPlugin : PluginBase
    {
        public override string Id => "core.stats";
        public override string DisplayName => "📊 内置统计器";
        public override string Description => "统计文件总数和总大小 (测试内置插件)";
        public override PluginType Type => PluginType.General;

        public override Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
        {
            // 统计插件不参与竞价，只观察
            // Stats plugin doesn't bid, only observes
            return Task.FromResult<RouteProposal?>(null);
        }

        public override Task OnFileDetectedAsync(IFileEntry file)
        {
            // 观察文件，更新统计
            // Observe file, update statistics
            int count = Storage?.Load<int>("TotalFiles", 0) ?? 0;
            long totalSize = Storage?.Load<long>("TotalSize", 0) ?? 0;
            count++;
            totalSize += file.SizeBytes;
            Storage?.Save("TotalFiles", count);
            Storage?.Save("TotalSize", totalSize);
            Storage?.Save("LastScanTime", DateTime.Now.ToString());

            Console.WriteLine($"[StatsPlugin] 累计：{count} 个文件，共 {FormatSize(totalSize)}");
            return Task.CompletedTask;
        }

        private string FormatSize(long bytes)
        {
            // 简单的字节格式化
            // Simple byte formatting
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
