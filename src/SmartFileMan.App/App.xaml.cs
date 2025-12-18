using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using SmartFileMan.App.Helpers;

namespace SmartFileMan.App
{
    public partial class App : Application
    {
        private readonly AppShell _shell;

        // 构造函数注入 AppShell
        public App(AppShell shell)
        {
            InitializeComponent();
            _shell = shell;

            // 【移除】不要在这里设置 MainPage = shell;
        }

        // 【新增】重写 CreateWindow 方法
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_shell);
        }
    }
}