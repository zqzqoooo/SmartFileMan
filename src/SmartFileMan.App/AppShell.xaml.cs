using SmartFileMan.Contracts;
using SmartFileMan.Core.Services;
using Microsoft.Maui.Controls;

namespace SmartFileMan.App
{
    /// <summary>
    /// 应用程序外壳：负责导航结构和动态加载插件菜单
    /// App Shell: Responsible for navigation structure and dynamically loading plugin menus
    /// </summary>
    public partial class AppShell : Shell
    {
        private readonly PluginManager _pluginManager;

        // 构造函数：注入插件管理器并初始化菜单
        // Constructor: Inject PluginManager and initialize the menu
        public AppShell(PluginManager pluginManager)
        {
            InitializeComponent();
            _pluginManager = pluginManager;

            // 加载侧边栏插件菜单
            // Load the sidebar plugin menu
            LoadPluginMenu();
        }

        /// <summary>
        /// 遍历已加载的插件并为每个插件创建侧边栏入口
        /// Iterate through loaded plugins and create a sidebar entry for each
        /// </summary>
        private void LoadPluginMenu()
        {
            foreach (var plugin in _pluginManager.Plugins)
            {
                // 创建飞出菜单项和选项卡
                // Create FlyoutItem and Tab
                var item = new FlyoutItem { Title = plugin.DisplayName };
                var tab = new Tab { Title = plugin.DisplayName };

                // 创建一个空页面作为 UI 占位符
                // Create an empty page as a UI placeholder
                var content = new ContentPage { Title = plugin.DisplayName };

                // 当页面显示时，按需加载插件的真实 UI
                // Load the actual plugin UI on-demand when the page appears
                content.Appearing += (s, e) =>
                {
                    // 检查该插件是否实现了 UI 接口
                    // Check if the plugin implements the UI interface
                    if (plugin is IPluginUI uiPlugin)
                    {
                        // 调用插件方法获取其自定义视图
                        // Call the plugin method to retrieve its custom view
                        var pluginView = uiPlugin.GetView();

                        // 将视图设置为当前页面的内容
                        // Set the view as the content of the current page
                        content.Content = pluginView;
                    }
                    else
                    {
                        // 对于没有界面的插件，显示默认提示信息
                        // For plugins without a UI, display a default prompt message
                        content.Content = new Label
                        {
                            Text = "此插件在后台运行，无配置界面。(This plugin runs in the background and has no configuration interface.)",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        };
                    }
                };

                // 将页面组装到导航结构中
                // Assemble the page into the navigation structure
                tab.Items.Add(content);
                item.Items.Add(tab);
                this.Items.Add(item);
            }
        }
    }
}