using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts
{
    /// <summary>
    /// 新的文件处理插件接口：支持“观察-竞价”模式
    /// New File Plugin Interface: Supports "Observe-Bid" pattern
    /// </summary>
    public interface IFilePlugin : IPlugin
    {
        /// <summary>
        /// 插件类型 (通用/专用)
        /// Plugin Type (General/Specific)
        /// </summary>
        PluginType Type { get; }

        /// <summary>
        /// 阶段一：观察与状态更新
        /// 插件可以在这里读取文件内容、更新内部状态 (如学习用户习惯)，但不应移动文件。
        /// Phase 1: Observation and State Update
        /// Plugins can read file content and update internal state here, but should not move files.
        /// </summary>
        Task OnFileDetectedAsync(IFileEntry file);

        /// <summary>
        /// 阶段二：出价
        /// 插件根据文件特征返回一个提案。如果不想处理该文件，返回 null。
        /// Phase 2: Bidding
        /// Plugins return a proposal based on file characteristics. Return null if not interested.
        /// </summary>
        Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file);
    }
}
