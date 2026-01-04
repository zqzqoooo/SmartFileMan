using System.Collections.ObjectModel;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Core.Services;

namespace SmartFileMan.App;

public partial class PluginManagementPage : ContentPage
{
    private readonly PluginManager _pluginManager;
    private readonly ISettingsService _settings;
    public ObservableCollection<IPlugin> Plugins { get; set; }

    public PluginManagementPage(PluginManager pluginManager, ISettingsService settings)
    {
        InitializeComponent();
        _pluginManager = pluginManager;
        _settings = settings;
        Plugins = new ObservableCollection<IPlugin>(_pluginManager.Plugins);
        PluginsList.ItemsSource = Plugins;
    }

    private async void OnPluginToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch s && s.BindingContext is IPlugin plugin)
        {
            plugin.IsEnabled = e.Value;
            await _settings.SetPluginEnabledAsync(plugin.Id, e.Value);
        }
    }

    private async void OnMoveUpClicked(object sender, EventArgs e)
    {
        if (sender is Button b && b.BindingContext is IPlugin plugin)
        {
            int index = Plugins.IndexOf(plugin);
            if (index > 0)
            {
                Plugins.Move(index, index - 1);
                await SaveOrder();
            }
        }
    }

    private async void OnMoveDownClicked(object sender, EventArgs e)
    {
        if (sender is Button b && b.BindingContext is IPlugin plugin)
        {
            int index = Plugins.IndexOf(plugin);
            if (index < Plugins.Count - 1)
            {
                Plugins.Move(index, index + 1);
                await SaveOrder();
            }
        }
    }

    private async Task SaveOrder()
    {
        var order = Plugins.Select(p => p.Id).ToList();
        await _settings.SetPluginOrderAsync(order);
    }
}
