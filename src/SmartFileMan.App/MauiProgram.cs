using Microsoft.Extensions.Logging;
using SmartFileMan.App.Services;
using SmartFileMan.Contracts.Services; // 契约接口
using SmartFileMan.Core.Services;      // PluginManager, LiteDbStorage
using SmartFileMan.Sdk.Services;       // SafeContext
using LiteDB;
using CommunityToolkit.Maui; // Include CommunityToolkit
using SmartFileMan.Contracts; // For IFileManager
using Serilog;            // Serilog
using Serilog.Events;     // Serilog Levels

namespace SmartFileMan.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // 1. 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(
                    Path.Combine(FileSystem.AppDataDirectory, "logs", "smartfileman-.txt"), 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30) 
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            
            // 2. 注入日志服务
            builder.Services.AddLogging(logging => logging.AddSerilog());

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Initialize CommunityToolkit
                .UseMauiCommunityToolkitMediaElement() // Initialize MediaElement
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
            // Use Connection=Shared to allow external tools (like LiteDB Studio) to open the DB while app is running
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "smartfileman.db");
            string connectionString = $"Filename={dbPath};Connection=Shared";
            
            builder.Services.AddSingleton<LiteDatabase>(s => 
            {
                try
                {
                    return new LiteDatabase(connectionString);
                }
                catch (IOException ex)
                {
                    // Catch file lock issues explicitly to guide the developer
                    throw new InvalidOperationException(
                        $"Failed to open database at '{dbPath}'.\n" +
                        "If you are using LiteDB Studio or another tool, please close it or ensure it uses 'Shared' mode.\n" +
                        "If you just updated code, please Restart the debugging session completely.", ex);
                }
            });

            // 1.5 设置服务 (SettingsService) - 新增
            builder.Services.AddSingleton<ISettingsService, SettingsService>();

            // 2. 交互提供者 (Dialogs, Toasts)
            builder.Services.AddSingleton<IInteractionProvider, MauiInteractionProvider>();

            // 3. 安全上下文 (SafeContext)
            // 依赖 IInteractionProvider
            builder.Services.AddSingleton<SafeContext>();

            // 4. 插件管理器 (PluginManager)
            // 依赖 LiteDatabase 和 SafeContext，负责为每个插件分配存储
            builder.Services.AddSingleton<PluginManager>();

            // 5. 文件管理器 (FileManager) - 新增
            // 负责调度和安全移动
            builder.Services.AddSingleton<IFileTracker, FileTracker>(); // 注册追踪服务 / Register Tracker Service
            builder.Services.AddSingleton<FileManager>();
            // 注册 IFileManager 接口，使其可以被解析
            builder.Services.AddSingleton<IFileManager>(s => s.GetRequiredService<FileManager>());

            // 5.5 文件监控服务 (FileWatcherService)
            builder.Services.AddSingleton<FileWatcherService>();

            // 6. 页面与 Shell
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<SettingsPage>();         // 新增
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}