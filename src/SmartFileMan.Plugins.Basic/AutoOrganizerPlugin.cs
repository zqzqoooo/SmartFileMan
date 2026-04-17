using Microsoft.Maui.Controls; // 引用 UI / Reference UI
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.UI;
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
    public class AutoOrganizerPlugin : PluginBase
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
            // Get file extension and convert to lowercase
            string ext = Path.GetExtension(file.FullPath).TrimStart('.').ToLower();

            // If there is no extension, classify as "Others"
            if (string.IsNullOrEmpty(ext)) ext = "Others";

            // Determine destination folder path (subfolder in the current directory)
            string destFolder = Path.Combine(file.DirectoryPath, ext, file.Name);

            // Construct proposal:
            // Target path: destFolder
            // Score: 50 (Base score）
            // Reason: "Archive by extension"
            var proposal = new RouteProposal(destFolder, 50, $"Archive by extension")
            {
                OnProcessingSuccess = async (originalEntry, finalPath, hash) =>
                {
                    // Update total historical organized count
                    int totalCount = Storage?.Load<int>("TotalOrganizedCount", 0) ?? 0;
                    totalCount++;
                    Storage?.Save("TotalOrganizedCount", totalCount);
                    Storage?.Save("LastRunTime", System.DateTime.Now.ToString("G"));

                    System.Diagnostics.Debug.WriteLine($"[插件日志] 文件已归档: {originalEntry.Name}, 历史总计: {totalCount} [Plugin Log] File archived: {originalEntry.Name}, Total so far: {totalCount}");
                    await Task.CompletedTask;
                }
            };

            return Task.FromResult<RouteProposal?>(proposal);
        }
    }
}
