namespace SmartFileMan.Contracts
{
    /// <summary>
    /// 归档路径提案：插件对某个文件的“报价单”
    /// Route Proposal: The "bid" from a plugin for a specific file
    /// </summary>
    public class RouteProposal
    {
        /// <summary>
        /// 建议的目标路径 (完整路径)
        /// Suggested destination path (full path)
        /// </summary>
        public string DestinationPath { get; }

        /// <summary>
        /// 信心分数 (0-100)
        /// Confidence Score (0-100)
        /// </summary>
        public int Score { get; }

        /// <summary>
        /// 提案说明 (用于日志或调试)
        /// Proposal description (for logging or debugging)
        /// </summary>
        public string Description { get; }

        public RouteProposal(string destinationPath, int score, string description = "")
        {
            DestinationPath = destinationPath;
            Score = score;
            Description = description;
        }
    }
}
