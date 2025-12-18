using System;
using Microsoft.Extensions.DependencyInjection;

namespace SmartFileMan.App.Helpers
{
    // 简单的 ServiceLocator 以便在 XAML 初始化时解析页面
    public static class ServiceLocator
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        public static T Get<T>() => (T)ServiceProvider!.GetRequiredService(typeof(T));
    }
}
