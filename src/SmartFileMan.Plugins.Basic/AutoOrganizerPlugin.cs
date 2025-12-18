using Microsoft.Maui.Controls; // 引用 UI
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Sdk;
using System.Collections.Generic;
using System.IO; // 引用 IO 以缩短代码
using System.Linq;
using System.Threading.Tasks;

namespace SmartFileMan.Plugins.Basic
{
    public class AutoOrganizerPlugin : PluginBase, IPluginUI
    {
        public override string Id => "com.smartfileman.basic.organizer";
        public override string DisplayName => "📂 智能归档大师";
        public override string Description => "自动按扩展名归档文件，并记录整理总数。";

        public override async Task ExecuteAsync(IList<IFileEntry> files)
        {
            if (files == null || files.Count == 0) return;

            // --- 修复 1: 处理 Storage 可能为空的情况 ---
            // 意思是：如果 Storage 是空的，就当做 0；如果不为空，就加载 TotalOrganizedCount
            int totalCount = Storage?.Load<int>("TotalOrganizedCount", 0) ?? 0;

            int currentBatchCount = 0;

            foreach (var file in files)
            {
                // --- 修复 2: 将 .Path 改为 .FullPath ---
                // 假设你的接口定义的是 FullPath。如果这里还报错，请检查 IFileEntry.cs
                string ext = Path.GetExtension(file.FullPath).TrimStart('.').ToLower();

                if (string.IsNullOrEmpty(ext)) ext = "其他";

                string destFolder = Path.Combine(Path.GetDirectoryName(file.FullPath) ?? "", ext);

                // 使用 SDK 提供的安全移动方法
                await Move(file, destFolder);
                currentBatchCount++;
            }

            // 更新总数
            totalCount += currentBatchCount;

            // 保存回数据库
            Storage?.Save("TotalOrganizedCount", totalCount);
            Storage?.Save("LastRunTime", System.DateTime.Now.ToString("G"));

            System.Diagnostics.Debug.WriteLine($"[插件日志] 本次整理: {currentBatchCount}, 历史总计: {totalCount}");
        }

        // 【新增】实现 GetView 方法
        public View GetView()
        {
            // 创建视图，并把自己的 Storage 传给视图
            // 这样视图就能读取这个插件专属的数据库了
            return new OrganizerView(this.Storage);
        }
    }
}