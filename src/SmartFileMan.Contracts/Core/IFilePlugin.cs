using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts.Core
{
    public interface IFilePlugin : IPlugin
    {
        /// <summary>
        /// Plugin Type (General/Specific)
        /// </summary>
        PluginType Type { get; }

        /// <summary>
        /// Phase Zero: Analyze the entire batch of files to build context.
        /// </summary>
        Task AnalyzeBatchAsync(BatchContext context);

        /// <summary>
        /// Phase 1: Observation and State Update
        /// Plugins can read file content and update internal state here, but should not move files.
        /// </summary>
        Task OnFileDetectedAsync(IFileEntry file);

        /// <summary>
        /// Phase 2: Bidding
        /// Plugins return a proposal based on file characteristics. Return null if not interested.
        /// </summary>
        Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file);
    }
}
