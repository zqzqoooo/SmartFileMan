using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Core;

namespace SmartFileMan.Contracts.Services
{
    public interface IPluginManager
    {
        IEnumerable<IPlugin> Plugins { get; }
        Task<RouteProposal?> GetBestRouteAsync(IFileEntry file); // For debug pluging test

        /// <summary>
        /// Runs a simulation of the bidding process, returning results from ALL plugins.
        /// </summary>
        Task<IList<BiddingResult>> SimulateBiddingAsync(IFileEntry file);

        /// <summary>
        /// Delete a plugin file from disk.
        /// </summary>
        void DeletePlugin(IPlugin plugin);

        /// <summary>
        /// Analyze a batch of files to allow plugins to build context before bidding.
        /// </summary>
        Task AnalyzeBatchAsync(BatchContext context);

        /// <summary>
        /// 获取指定插件的存储数据（JSON 格式调试用）
        /// Get storage data for a specific plugin (JSON format for debugging)
        /// </summary>
        Task<string> GetPluginStorageDumpAsync(string collectionName);

        /// <summary>
        /// 执行 SQL 查询
        /// Execute SQL Query
        /// </summary>
        Task<string> ExecuteQueryAsync(string sql);

        /// <summary>
        /// 获取所有数据库集合名称
        /// Get all database collection names
        /// </summary>
        IEnumerable<string> GetDatabaseCollections();

        /// <summary>
        /// 清除所有插件存储和追踪数据
        /// Clear all plugin storage and tracking data
        /// </summary>
        Task ClearAllDataAsync();
    }
}
