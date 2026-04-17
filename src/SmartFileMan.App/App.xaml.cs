using System;
using Microsoft.Extensions.DependencyInjection;
using SmartFileMan.App.Helpers;
using SmartFileMan.App.Services;

namespace SmartFileMan.App;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly FileWatcherService _watcherService;

    // 让 MAUI 把 mainPage 注入进来
    public App(AppShell shell, IServiceProvider serviceProvider, FileWatcherService watcherService)
    {
        InitializeComponent();
        _shell = shell;
        ServiceLocator.ServiceProvider = serviceProvider;
        _watcherService = watcherService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // 启动时初始化监控
        _watcherService.InitializeAsync().ConfigureAwait(false);
        return new Window(_shell);
    }
}