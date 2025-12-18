using SmartFileMan.Contracts;
using SmartFileMan.Core.Services;
using SmartFileMan.Sdk;
using SmartFileMan.Sdk.Services;

namespace SmartFileMan.App
{
    public partial class MainPage : ContentPage
    {
        private readonly PluginManager _pluginManager;
        private readonly SafeContext _safeContext;

        public MainPage(PluginManager pluginManager, SafeContext safeContext)
        {
            InitializeComponent();
            _pluginManager = pluginManager;
            _safeContext = safeContext;
        }



        // 【关键点】这里必须叫 OnStartClicked，和 XAML 里的 Clicked 属性完全一致
        private async void OnStartClicked(object sender, EventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                var scanner = new FileScanner();
                var files = await scanner.ScanAsync(path);

                await _pluginManager.RunAllPluginsAsync(files);

                await DisplayAlert("成功", $"处理完成！共扫描 {files.Count} 个文件。", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"执行过程中出错: {ex.Message}", "OK");
            }
        }

        public void ReplaceMainContent(View newContent)
        {
            MainContainer.Children.Clear();
            MainContainer.Children.Add(newContent);
        }
    }
}