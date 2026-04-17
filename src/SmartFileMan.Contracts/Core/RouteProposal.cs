using System;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts.Core
{
    /// <summary>
    /// Route Proposal: The "bid" from a plugin for a specific file
    /// </summary>
    public class RouteProposal
    {
        /// <summary>
        /// Suggested destination path
        /// </summary>
        public string DestinationPath { get; }

        /// <summary>
        /// Confidence Score
        /// </summary>
        public int Score { get; }

        /// <summary>
        /// Proposal description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Plugin name
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Plugin Id
        /// </summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>
        /// Callback to execute upon processing success
        /// </summary>
        public Func<IFileEntry, string, string, Task>? OnProcessingSuccess { get; set; }

        // Alias for Description to satisfy requirement/usage
        public string Explanation => Description;

        public RouteProposal(string destinationPath, int score, string description = "")
        {
            DestinationPath = destinationPath;
            Score = score;
            Description = description;
        }
    }
}
