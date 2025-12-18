using Microsoft.Maui.Controls; // 需要 MAUI 控件支持

namespace SmartFileMan.Contracts
{
    /// <summary>
    /// 实现了这个接口的插件，说明它拥有自己的主界面
    /// </summary>
    public interface IPluginUI : IPlugin
    {
        /// <summary>
        /// 获取插件的主界面视图
        /// </summary>
        /// <returns>返回一个 View (比如 Grid, StackLayout, ContentView)</returns>
        View GetMainView();
    }
}