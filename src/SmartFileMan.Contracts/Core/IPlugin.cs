namespace SmartFileMan.Contracts.Core
{
    public interface IPlugin
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        string Version { get; }
        bool IsEnabled { get; set; }
    }
}
