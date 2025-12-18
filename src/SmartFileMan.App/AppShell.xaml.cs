using SmartFileMan.Contracts;
using SmartFileMan.Core.Services;

namespace SmartFileMan.App
{
    public partial class AppShell : Shell
    {
        private readonly PluginManager _pluginManager;

        public AppShell(PluginManager pluginManager)
        {
            InitializeComponent();
            _pluginManager = pluginManager;

            // 加载侧边栏菜单
            LoadPluginMenu();
        }

        private void LoadPluginMenu()
        {
            foreach (var plugin in _pluginManager.Plugins)
            {
                var item = new FlyoutItem { Title = plugin.DisplayName };
                var tab = new Tab { Title = plugin.DisplayName };

                // 创建一个空页面作为占位符
                var content = new ContentPage { Title = plugin.DisplayName };

                // 【核心逻辑】当页面显示时，加载插件的真实 UI
                content.Appearing += (s, e) =>
                {
                    // 1. 检查这个插件是否支持 UI
                    if (plugin is IPluginUI uiPlugin)
                    {
                        // 2. 获取插件的 View
                        var pluginView = uiPlugin.GetView();
                        // 3. 设置为当前页面的内容
                        content.Content = pluginView;
                    }
                    else
                    {
                        // 没有界面的插件显示默认提示
                        content.Content = new Label
                        {
                            Text = "此插件在后台运行，无配置界面。",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        };
                    }
                };

                tab.Items.Add(content);
                item.Items.Add(tab);
                this.Items.Add(item);
            }
        }
    }
}