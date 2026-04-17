using SmartFileMan.App.Helpers;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.UI;
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
            
            // Subscribe to hot-reload
            _pluginManager.PluginsChanged += (s, e) => MainThread.BeginInvokeOnMainThread(LoadPluginMenu);

            // Initial load
            LoadPluginMenu();
        }

        /// <summary>
        /// 遍历已加载的插件并为每个插件创建侧边栏入口
        /// Iterate through loaded plugins and create a sidebar entry for each
        /// </summary>
        private void LoadPluginMenu()
        {
            // Capture current navigation state
            var currentTitle = Current?.CurrentItem?.Title;

            // Clear existing plugin items (Naive approach: Clear all and re-add fixed items + plugins)
            // Ideally we should track which items are plugins.
            this.Items.Clear();

            // Re-add Start Page (if needed) or hardcoded items in XAML?
            // Since this.Items.Clear() removes XAML items too if they are added to Items collection.
            // Assuming AppShell.xaml is mostly empty or we re-construct.
            
            // Add Default/Home Item
            var startItem = new FlyoutItem { Title = "Home", Icon = "home_icon.png" }; // Icon optional
            var startTab = new Tab { Title = "Home" };
            startTab.Items.Add(new ShellContent { Title = "Home", ContentTemplate = new DataTemplate(typeof(MainPage)) });
            startItem.Items.Add(startTab);
            this.Items.Add(startItem);

            foreach (var plugin in _pluginManager.Plugins)
            {
                if (plugin is IPluginUI uiPlugin && plugin.IsEnabled)
                {
                    // Create FlyoutItem and Tab
                    var item = new FlyoutItem { Title = plugin.DisplayName };
                    var tab = new Tab { Title = plugin.DisplayName };

                    // Create an empty page as a UI placeholder
                    var content = new ContentPage { Title = plugin.DisplayName };

                    // Load the actual plugin UI on-demand when the page appears
                    content.Appearing += (s, e) =>
                    {
                        // Check if the plugin implements the UI interface
                        if (plugin is IPluginUI uiPlugin)
                        {
                            // Call the plugin method to retrieve its custom view
                            var pluginView = uiPlugin.GetView();

                            // Set the view as the content of the current page
                            content.Content = pluginView;
                        }
                        else
                        {
                            // For plugins without a UI, display a default prompt message
                            content.Content = new Label
                            {
                                Text = "This plugin runs in the background and has no configuration interface.",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            };
                        }
                    };

                    // Assemble the page into the navigation structure
                    tab.Items.Add(content);
                    item.Items.Add(tab);
                    this.Items.Add(item);
                }
            }

            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            
            // Restore navigation
            if (currentTitle != null)
            {
                var match = this.Items.FirstOrDefault(i => i.Title == currentTitle);
                if (match != null) 
                {
                    // If we are on main thread, set it
                    try { CurrentItem = match; } catch { }
                }
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // Close flyout and navigate
            Current.FlyoutIsPresented = false;
            await Current.GoToAsync(nameof(SettingsPage));
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            Application.Current.Quit();
        }
    }
}