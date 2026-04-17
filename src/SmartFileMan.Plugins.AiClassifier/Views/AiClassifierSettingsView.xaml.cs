using SmartFileMan.Plugins.AiClassifier.Models;
using SmartFileMan.Plugins.AiClassifier.Services;
using System.Collections.ObjectModel;

namespace SmartFileMan.Plugins.AiClassifier.Views;

public partial class AiClassifierSettingsView : ContentPage
{
    private readonly AiClassifierPlugin _plugin;

    public AiClassifierSettingsView(AiClassifierPlugin plugin)
    {
        InitializeComponent();
        _plugin = plugin;

        LoadConfig();

        TemperatureSlider.ValueChanged += (s, e) =>
        {
            TemperatureLabel.Text = e.NewValue.ToString("F1");
        };
    }

    private void LoadConfig()
    {
        var config = _plugin.GetLlmConfig();
        ProviderPicker.SelectedIndex = config.Provider == LlmProviderType.MiniMax ? 0 : 1;
        EndpointEntry.Text = config.Endpoint;
        ApiKeyEntry.Text = config.ApiKey;
        ModelEntry.Text = config.Model;
        TemperatureSlider.Value = config.Temperature;
        TemperatureLabel.Text = config.Temperature.ToString("F1");
        CustomPromptEditor.Text = config.CustomPrompt;
        UpdateEndpointPlaceholder();
    }

    private void ProviderPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateEndpointPlaceholder();
    }

    private void UpdateEndpointPlaceholder()
    {
        if (ProviderPicker.SelectedIndex == 0)
        {
            EndpointEntry.Placeholder = "https://api.minimaxi.com/v1/messages";
            ModelEntry.Placeholder = "MiniMax-M2.7";
        }
        else
        {
            EndpointEntry.Placeholder = "https://api.openai.com/v1/chat/completions";
            ModelEntry.Placeholder = "gpt-4o";
        }
    }

    private LlmConfig GetCurrentConfig()
    {
        bool isMiniMax = ProviderPicker.SelectedIndex == 0;
        return new LlmConfig
        {
            Provider = isMiniMax ? LlmProviderType.MiniMax : LlmProviderType.OpenAI,
            Endpoint = string.IsNullOrWhiteSpace(EndpointEntry.Text) 
                ? (isMiniMax ? "https://api.minimaxi.com/v1/messages" : "https://api.openai.com/v1/chat/completions") 
                : EndpointEntry.Text.Trim(),
            ApiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty,
            Model = string.IsNullOrWhiteSpace(ModelEntry.Text) 
                ? (isMiniMax ? "MiniMax-M2.7" : "gpt-4o-mini") 
                : ModelEntry.Text.Trim(),
            Temperature = TemperatureSlider.Value,
            CustomPrompt = string.IsNullOrWhiteSpace(CustomPromptEditor.Text) 
                ? "请自行根据文件类型和名称推断，将文件分类到合适的文件夹中" 
                : CustomPromptEditor.Text.Trim()
        };
    }

    private async void TestConnection_Clicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "正在测试连接...";
        StatusLabel.TextColor = Colors.Orange;
        DebugInfoBorder.IsVisible = false;

        var config = GetCurrentConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            StatusLabel.Text = "API Key 不能为空";
            StatusLabel.TextColor = Colors.Red;
            return;
        }

        _plugin.SaveLlmConfig(config);

        var result = await _plugin.TestLlmConnectionAsync();
        bool success = result.Success;

        StatusLabel.Text = success ? "连接成功!" : $"连接失败，请检查配置 ({result.ErrorMessage})";
        StatusLabel.TextColor = success ? Colors.Green : Colors.Red;

        // Populate debug text
        RequestInfoLabel.Text = result.RawRequest ?? $"Endpoint: {config.Endpoint}\nTarget Model: {config.Model ?? "Default"}";
        ResponseInfoLabel.Text = string.IsNullOrWhiteSpace(result.RawResponse) ? "No response or connection failed." : result.RawResponse;

        // Show debug info
        DebugInfoBorder.IsVisible = true;
    }

    private void SaveConfig_Clicked(object sender, EventArgs e)
    {
        var config = GetCurrentConfig();

        _plugin.SaveLlmConfig(config);

        StatusLabel.Text = "配置已保存";
        StatusLabel.TextColor = Colors.Green;
    }
}
