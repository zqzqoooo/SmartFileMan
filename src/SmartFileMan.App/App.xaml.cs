using System;
using Microsoft.Extensions.DependencyInjection;
using SmartFileMan.App.Helpers;

namespace SmartFileMan.App;

public partial class App : Application
{
    private readonly AppShell _shell;

    // 让 MAUI 把 mainPage 注入进来
    public App(AppShell shell, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _shell = shell;
        ServiceLocator.ServiceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }
}