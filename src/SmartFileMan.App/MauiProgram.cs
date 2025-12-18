using Microsoft.Extensions.Logging;
using SmartFileMan.App.Services;
using SmartFileMan.Contracts.Services; // 契约接口
using SmartFileMan.Core.Services;      // PluginManager, LiteDbStorage
using SmartFileMan.Sdk.Services;       // SafeContext
using LiteDB;

namespace SmartFileMan.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // --- 服务注册 ---

            // 1. 数据存储根服务 (LiteDatabase)
            // 整个 App 共享一个数据库连接，LiteDB 是线程安全的
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "smartfileman.db");
            builder.Services.AddSingleton<LiteDatabase>(s => new LiteDatabase(dbPath));

            // 2. 交互提供者 (Dialogs, Toasts)
            builder.Services.AddSingleton<IInteractionProvider, MauiInteractionProvider>();

            // 3. 安全上下文 (SafeContext)
            // 依赖 IInteractionProvider
            builder.Services.AddSingleton<SafeContext>();

            // 4. 插件管理器 (PluginManager)
            // 依赖 LiteDatabase 和 SafeContext，负责为每个插件分配存储
            builder.Services.AddSingleton<PluginManager>();

            // 5. 页面与 Shell
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}