using Microsoft.Maui.Controls; // 引用 UI / Reference UI
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Sdk;
using System.Collections.Generic;
using System.IO; // 引用 IO 以缩短代码 / Reference IO to shorten code
using System.Linq;
using System.Threading.Tasks;

namespace SmartFileMan.Plugins.Basic
{
    /// <summary>
    /// 自动归档插件：实现基础整理逻辑与用户界面接口
    /// Auto Organizer Plugin: Implements basic sorting logic and UI interface
    /// </summary>
    public class AutoOrganizerPlugin : PluginBase, IPluginUI
    {
        // 插件唯一标识符
        // Unique identifier for the plugin
        public override string Id => "com.smartfileman.basic.organizer";

        // 插件显示名称
        // Display name of the plugin
        public override string DisplayName => "📂 智能归档大师 Smart Archiving Master";

        // 插件功能描述
        // Functional description of the plugin
        public override string Description => "自动按扩展名归档文件，并记录整理总数。Automatically archive files by extension and keep a record of the total sorted.";

        /// <summary>
        /// 阶段二：竞价逻辑
        /// Phase 2: Bidding logic
        /// </summary>
        public override Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
        {
            // 获取文件扩展名并转为小写
            // Get file extension and convert to lowercase
            string ext = Path.GetExtension(file.FullPath).TrimStart('.').ToLower();

            // 如果没有扩展名，归类为 "其他"
            // If there is no extension, classify as "Others"
            if (string.IsNullOrEmpty(ext)) ext = "其他 Others";

            // 确定目标文件夹路径 (当前目录下的子文件夹)
            // Determine destination folder path (subfolder in the current directory)
            string destFolder = Path.Combine(Path.GetDirectoryName(file.FullPath) ?? "", ext);

            // 构造提案：
            // Construct proposal:
            // 目标路径: destFolder
            // Target path: destFolder
            // 分数: 50 (基础分，表示我可以处理，但优先级不高)
            // Score: 50 (Base score, indicates I can handle it, but priority is not high)
            // 理由: "按扩展名归档"
            // Reason: "Archive by extension"
            var proposal = new RouteProposal(destFolder, 50, $"按扩展名 {ext} 归档");

            return Task.FromResult<RouteProposal?>(proposal);
        }

        /// <summary>
        /// 核心执行逻辑：按扩展名移动文件 (旧版兼容)
        /// Core execution logic: Move files by extension (Legacy compatibility)
        /// </summary>
        /// <param name="files">待处理文件列表 / List of files to be processed</param>
        public override async Task ExecuteAsync(IList<IFileEntry> files)
        {
            if (files == null || files.Count == 0) return;

            // --- 处理存储读取：如果 Storage 为空则默认为 0 ---
            // Handle storage reading: Default to 0 if Storage is null
            int totalCount = Storage?.Load<int>("TotalOrganizedCount", 0) ?? 0;

            int currentBatchCount = 0;

            foreach (var file in files)
            {
                // 获取文件扩展名并转为小写
                // Get file extension and convert to lowercase
                string ext = Path.GetExtension(file.FullPath).TrimStart('.').ToLower();

                // 处理无扩展名的情况
                // Handle cases with no extension
                if (string.IsNullOrEmpty(ext)) ext = "其他 Others";

                // 确定目标文件夹路径
                // Determine destination folder path
                string destFolder = Path.Combine(Path.GetDirectoryName(file.FullPath) ?? "", ext);

                // 使用 SDK 提供的安全移动方法执行操作
                // Use the safe move method provided by the SDK to perform the operation
                await Move(file, destFolder);
                currentBatchCount++;
            }

            // 更新历史整理总数
            // Update total historical organized count
            totalCount += currentBatchCount;

            // 将最新数据保存回插件专属数据库
            // Save latest data back to the plugin-specific database
            Storage?.Save("TotalOrganizedCount", totalCount);
            Storage?.Save("LastRunTime", System.DateTime.Now.ToString("G"));

            // 输出调试日志
            // Output debug log
            System.Diagnostics.Debug.WriteLine($"[插件日志] 本次整理: {currentBatchCount}, 历史总计: {totalCount} [Plugin Log] This batch processed: {currentBatchCount}, Total so far: {totalCount}");
        }

        /// <summary>
        /// 获取插件设置或状态视图
        /// Get the plugin settings or status view
        /// </summary>
        /// <returns>MAUI 视图组件 / MAUI View component</returns>
        public View GetView()
        {
            // 创建视图，并将当前的存储实例注入视图
            // Create view and inject the current storage instance into it
            return new OrganizerView(this.Storage);
        }
    }
}