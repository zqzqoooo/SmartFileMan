using Microsoft.Maui.Controls;

namespace SmartFileMan.Plugins.AiClassifier.Views;

/// <summary>
/// AI 智能分类插件入口视图
/// Entry view for AI Classifier Plugin
/// </summary>
public class AiClassifierEntryView : ContentView
{
    public AiClassifierEntryView(AiClassifierPlugin plugin)
    {
        var stackLayout = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 10
        };

        var titleLabel = new Label
        {
            Text = "AI Intelligent Classification",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold
        };

        var descLabel = new Label
        {
            Text = "基于 LLM 的语义文件分类插件\n点击按钮配置 LLM 和分类规则",
            FontSize = 14,
            TextColor = Colors.Gray
        };

        var openButton = new Button
        {
            Text = "Open settings",
            Margin = new Thickness(0, 10, 0, 0)
        };

        openButton.Clicked += async (s, e) =>
        {
            var settingsView = new AiClassifierSettingsView(plugin);
            await Application.Current!.MainPage!.Navigation.PushAsync(settingsView);
        };

        stackLayout.Children.Add(titleLabel);
        stackLayout.Children.Add(descLabel);
        stackLayout.Children.Add(openButton);

        Content = stackLayout;
    }
}
