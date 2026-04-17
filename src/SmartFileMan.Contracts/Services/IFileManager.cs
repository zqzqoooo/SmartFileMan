using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts.Services
{
    public interface IFileManager
    {
        Task<OperationResult> ProcessFileAsync(IFileEntry file);

        /// <summary>
        /// Process a batch of files together (Analyze Context -> Bid -> Execute).
        /// </summary>
        Task<OperationResult> ProcessBatchAsync(IEnumerable<IFileEntry> files);
    }
}
