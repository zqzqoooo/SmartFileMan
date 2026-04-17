using System.Collections.Generic;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts.Core
{
    public class BatchContext
    {
        public string BatchId { get; }
        /// <summary>
        /// A read-only list of all files in the current processing batch.
        /// Plugins can analyze this list to build internal context/relationships.
        /// </summary>
        public IReadOnlyList<IFileEntry> AllFiles { get; }

        public BatchContext(string batchId, IReadOnlyList<IFileEntry> allFiles)
        {
            BatchId = batchId;
            AllFiles = allFiles;
        }
    }
}
