namespace SmartFileMan.Contracts
{
    /// <summary>
    /// 插件类型：决定了插件在竞价时的优先级
    /// Plugin Type: Determines the priority of the plugin during bidding
    /// </summary>
    public enum PluginType
    {
        /// <summary>
        /// 通用插件 (如: 按日期归档) - 优先级较低
        /// General Plugin (e.g., Archive by Date) - Lower priority
        /// </summary>
        General = 0,

        /// <summary>
        /// 专用插件 (如: 身份证识别、发票归档) - 优先级较高
        /// Specific Plugin (e.g., ID Card Recognition, Invoice Archiving) - Higher priority
        /// </summary>
        Specific = 1
    }
}
