using System;
using SmartFileMan.Sdk.Services;
using SmartFileMan.Core.Services;
using Microsoft.Maui.Controls;
using SmartFileMan.App.Helpers;
using SmartFileMan.Core.Models; // For FileScanner if needed, or just System.IO
using System.IO;
using System.Linq;

namespace SmartFileMan.App;

public partial class MainPage : ContentPage
{
    private readonly FileManager _fileManager;
    private readonly SafeContext _safeContext;

    // Parameterless ctor for XAML instantiation - resolves dependencies via ServiceLocator
    public MainPage() : this(ServiceLocator.Get<FileManager>(), ServiceLocator.Get<SafeContext>())
    {
    }

    // 通过构造函数注入 FileManager 和 SafeContext
    public MainPage(FileManager fileManager, SafeContext safeContext)
    {
        InitializeComponent();
        _fileManager = fileManager;
        _safeContext = safeContext;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        try
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // 简单扫描桌面文件 (Simple scan of desktop files)
            var files = Directory.GetFiles(path).Select(f => new LocalFileEntry(f)).ToList();

            int processedCount = 0;
            foreach (var file in files)
            {
                var result = await _fileManager.ProcessFileAsync(file);
                if (result.IsSuccess)
                {
                    processedCount++;
                }
            }

            await DisplayAlert("完成 (Done)", $"处理完成！共处理 {processedCount} 个文件。\nProcessed {processedCount} files.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误 (Error)", $"执行出错: {ex.Message}", "OK");
        }
    }

    private async void OnUndoClicked(object sender, EventArgs e)
    {
        await _safeContext.UndoLastActionAsync();
        await DisplayAlert("撤销 (Undo)", "已尝试撤销上一步操作。\nAttempted to undo last action.", "OK");
    }
}