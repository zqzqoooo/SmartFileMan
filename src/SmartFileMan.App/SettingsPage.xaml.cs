using System.Collections.ObjectModel;
using SmartFileMan.Contracts.Services;

namespace SmartFileMan.App;

public partial class SettingsPage : ContentPage
{
    private readonly ISettingsService _settings;
    public ObservableCollection<string> IgnoredExtensions { get; set; } = new();

    public SettingsPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        ExtList.ItemsSource = IgnoredExtensions;
        LoadData();
    }

    private async void LoadData()
    {
        var list = await _settings.GetIgnoredExtensionsAsync();
        IgnoredExtensions.Clear();
        foreach (var item in list) IgnoredExtensions.Add(item);

        DevModeSwitch.IsToggled = _settings.IsDeveloperModeEnabled();
    }

    private async void OnAddExtClicked(object sender, EventArgs e)
    {
        string ext = ExtEntry.Text?.Trim().ToLower();
        if (!string.IsNullOrEmpty(ext))
        {
            if (!ext.StartsWith(".")) ext = "." + ext;
            
            if (!IgnoredExtensions.Contains(ext))
            {
                IgnoredExtensions.Add(ext);
                await _settings.AddIgnoredExtensionAsync(ext);
                ExtEntry.Text = "";
            }
        }
    }

    private async void OnRemoveExtClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string ext)
        {
            IgnoredExtensions.Remove(ext);
            await _settings.RemoveIgnoredExtensionAsync(ext);
        }
    }

    private async void OnDevModeToggled(object sender, ToggledEventArgs e)
    {
        await _settings.SetDeveloperModeEnabledAsync(e.Value);
    }
}
