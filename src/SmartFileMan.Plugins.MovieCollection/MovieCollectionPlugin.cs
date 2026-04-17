using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using SmartFileMan.Sdk;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.UI;
using SmartFileMan.Contracts.Storage;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;
using SmartFileMan.Plugins.MovieCollection.ViewModels;
using SmartFileMan.Plugins.MovieCollection.Views;
using SmartFileMan.Plugins.MovieCollection.Views.Components;

namespace SmartFileMan.Plugins.MovieCollection;

/// <summary>
/// 影视合集管理插件
/// Movie collection management plugin for SmartFileMan
/// </summary>
public class MovieCollectionPlugin : PluginBase, IPluginUI
{
    private readonly IBatchModule _batchModule;
    private ICacheService _cacheService = null!;
    private ITmdbService? _tmdbService;
    private IRenameService? _renameService;

    private DetailPageViewModel? _detailViewModel;
    private SearchResultViewModel? _searchViewModel;
    private SettingsViewModel? _settingsViewModel;
    private BatchProcessViewModel? _batchViewModel;

    private ContentView? _currentView;
    private int _currentTmdbId;
    private string _currentMediaType = "tv";

    private Grid _mainContainer = new Grid();
    private LibraryPage? _libraryPage;
    private LibraryViewModel? _libraryViewModel;
    private DetailPage? _detailPage;

    /// <summary>
    /// 插件唯一标识符
    /// Plugin unique identifier
    /// </summary>
    public override string Id => "com.smartfileman.plugin.moviecollection";

    /// <summary>
    /// 插件显示名称
    /// Plugin display name
    /// </summary>
    public override string DisplayName => "影视合集管理";

    /// <summary>
    /// 插件描述
    /// Plugin description
    /// </summary>
    public override string Description => "自动识别并重命名电视剧、电影文件，通过TMDB获取影视信息";

    /// <summary>
    /// 插件类型
    /// Plugin type - Specific has higher priority than General
    /// </summary>
    public override PluginType Type => PluginType.Specific;

    /// <summary>
    /// 插件配置
    /// Plugin configuration
    /// </summary>
    private PluginConfig Config => _cacheService?.GetConfig() ?? new PluginConfig();

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    public MovieCollectionPlugin()
    {
        _batchModule = new BatchModule();
    }

    /// <summary>
    /// 插件初始化完成时的回调
    /// Callback when plugin initialization is complete
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _cacheService = new CacheService(Storage!);
    }

    /// <summary>
    /// 初始化TMDB服务
    /// Initialize TMDB service
    /// </summary>
    private void InitializeTmdbService()
    {
        var apiKey = Config.TmdbApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _tmdbService = new TmdbService(apiKey);
        }
    }

    /// <inheritdoc />
    public override async Task AnalyzeBatchAsync(BatchContext context)
    {
        var indices = await _batchModule.AnalyzeBatchAsync(context);

        foreach (var index in indices)
        {
            _cacheService.SaveFileIndex(index);
        }
    }

    /// <inheritdoc />
    public override async Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
    {
        if (string.IsNullOrEmpty(Config.TmdbApiKey))
            return null;

        var extension = file.Extension?.ToLowerInvariant();
        if (extension != ".mkv" && extension != ".mp4" && extension != ".avi")
            return null;

        InitializeTmdbService();

        try
        {
            var mediaType = _batchModule.RecognizeMediaType(file.Name);
            var keyword = _batchModule.GenerateSearchKeyword(file.Name);

            var results = await _tmdbService!.SearchAsync(keyword, "all");

            if (results.Count == 0)
                return null;

            var matched = results[0];

            if (mediaType == "tv")
            {
                var episodeInfo = _batchModule.ExtractEpisodeInfo(file.Name);
                if (episodeInfo != null)
                {
                    var fullDetails = await _tmdbService.GetTvDetailsAsync(matched.TmdbId);
                    if (fullDetails != null)
                    {
                        var template = Config.RenameTemplate;
                        var renameService = new RenameService();

                        var newName = renameService.FormatEpisodeTitle(template, fullDetails, new EpisodeInfo
                        {
                            SeasonNumber = episodeInfo.SeasonNumber,
                            EpisodeNumber = episodeInfo.EpisodeNumber,
                            Name = $"Episode {episodeInfo.EpisodeNumber}"
                        });

                        newName = $"{newName}{extension}";

                        var targetFolder = Config.TargetFolder;
                        var fileDir = System.IO.Path.GetDirectoryName(file.FullPath) ?? "";

                        string directory = string.IsNullOrWhiteSpace(targetFolder) ? fileDir : targetFolder;

                        if (!System.IO.Path.IsPathRooted(directory) && !string.IsNullOrEmpty(fileDir))
                        {
                            directory = System.IO.Path.Combine(fileDir, directory);
                        }

                        if (!string.IsNullOrEmpty(directory))
                        {
                            directory = System.IO.Path.GetFullPath(directory);
                        }

                        var newPath = System.IO.Path.Combine(directory, newName);

                        return new RouteProposal(newPath, 85, $"匹配: {fullDetails.Title} S{episodeInfo.SeasonNumber:00}E{episodeInfo.EpisodeNumber:00}");
                    }
                }
            }

            return new RouteProposal(file.FullPath, 50, "Video File");
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override Task OnFileDetectedAsync(IFileEntry file)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取插件视图
    /// Get plugin view for UI display
    /// </summary>
    public View GetView()
    {
        if (_currentView == null)
        {
            _currentView = CreateMainView();
        }

        return _currentView;
    }

    private View? _navBar;
    private SettingsPage? _settingsPage;
    private SearchResultPage? _searchPage;

    /// <summary>
    /// 创建主视图
    /// Create main view
    /// </summary>
    private ContentView CreateMainView()
    {
        InitializeTmdbService();

        _renameService = new RenameService();

        _detailViewModel = new DetailPageViewModel(_tmdbService!, _cacheService, _renameService);
        _detailViewModel.OnGoBack = () => NavigateToLibrary();

        _searchViewModel = new SearchResultViewModel(_tmdbService!, _cacheService);
        _searchViewModel.MediaSelected += OnMediaSelected;

        _settingsViewModel = new SettingsViewModel(_cacheService);
        _settingsViewModel.SettingsSaved += (s, e) => NavigateToLibrary();

        _batchViewModel = new BatchProcessViewModel(_batchModule, _tmdbService!, _cacheService, _renameService);

        _libraryViewModel = new LibraryViewModel(_cacheService, _tmdbService);
        _libraryViewModel.LibraryItemSelected += OnLibraryItemSelected;

        _libraryPage = new LibraryPage { BindingContext = _libraryViewModel };
        _detailPage = new DetailPage { BindingContext = _detailViewModel };
        _settingsPage = new SettingsPage { BindingContext = _settingsViewModel };
        _searchPage = new SearchResultPage { BindingContext = _searchViewModel };

        // Create Navigation Bar
        var btnLibrary = new Button { Text = "🎬 海报墙 / Library", BackgroundColor = Colors.Transparent, TextColor = Colors.White };
        var btnSearch = new Button { Text = "🔍 搜索 / Search", BackgroundColor = Colors.Transparent, TextColor = Colors.White };
        var btnSettings = new Button { Text = "⚙️ 设置 / Settings", BackgroundColor = Colors.Transparent, TextColor = Colors.White };

        btnLibrary.Clicked += (s, e) => NavigateToLibrary();
        btnSearch.Clicked += (s, e) => { _mainContainer.Children.Clear(); _mainContainer.Children.Add(_searchPage); };
        btnSettings.Clicked += (s, e) => { _mainContainer.Children.Clear(); _mainContainer.Children.Add(_settingsPage); };

        _navBar = new HorizontalStackLayout
        {
            Spacing = 15,
            Padding = new Thickness(10),
            BackgroundColor = Color.FromArgb("#0F3460"),
            Children = { btnLibrary, btnSearch, btnSettings }
        };

        var rootGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            BackgroundColor = Color.FromArgb("#1A1A2E")
        };

        rootGrid.Children.Add((View)_navBar);
        Grid.SetRow((View)_navBar, 0);

        rootGrid.Children.Add(_mainContainer);
        Grid.SetRow(_mainContainer, 1);

        NavigateToLibrary();

        return new ContentView { Content = rootGrid };
    }

    private void NavigateToLibrary()
    {
        _mainContainer.Children.Clear();
        _mainContainer.Children.Add(_libraryPage);
        _libraryViewModel?.LoadLibrary();
    }

    private void OnLibraryItemSelected(object? sender, LibraryItemViewModel item)
    {
        _currentTmdbId = item.TmdbId;
        _currentMediaType = item.MediaTypeLabel == "电影" ? "movie" : "tv";

        _mainContainer.Children.Clear();
        _mainContainer.Children.Add(_detailPage);

        _ = _detailViewModel?.LoadDetailsAsync(item.TmdbId);
    }

    private void OnMediaSelected(object? sender, MediaInfo media)
    {
        _currentTmdbId = media.TmdbId;
        _currentMediaType = media.MediaType;

        _mainContainer.Children.Clear();
        _mainContainer.Children.Add(_detailPage);

        if (_detailViewModel != null)
        {
            _ = _detailViewModel.LoadDetailsAsync(media.TmdbId);
        }
    }

    /// <summary>
    /// 加载设置视图
    /// Load settings view
    /// </summary>
    public View GetSettingsView()
    {
        if (_settingsViewModel == null)
        {
            _settingsViewModel = new SettingsViewModel(_cacheService);
        }

        return new SettingsPage
        {
            BindingContext = _settingsViewModel
        };
    }

    /// <summary>
    /// 加载搜索视图
    /// Load search view
    /// </summary>
    public View GetSearchView()
    {
        if (_searchViewModel == null)
        {
            _searchViewModel = new SearchResultViewModel(_tmdbService!, _cacheService);
            _searchViewModel.MediaSelected += OnMediaSelected;
        }

        return new SearchResultPage
        {
            BindingContext = _searchViewModel
        };
    }
}
