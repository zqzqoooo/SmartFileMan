using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;

namespace SmartFileMan.Plugins.MovieCollection.ViewModels;

/// <summary>
/// 搜索结果视图模型
/// Search result view model
/// </summary>
public class SearchResultViewModel : ViewModelBase
{
    private readonly ITmdbService _tmdbService;
    private readonly ICacheService _cacheService;

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    private ObservableCollection<MediaInfo> _searchResults = new();
    public ObservableCollection<MediaInfo> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    private MediaInfo? _selectedMedia;
    public MediaInfo? SelectedMedia
    {
        get => _selectedMedia;
        set => SetProperty(ref _selectedMedia, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _hasSearched;
    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private Dictionary<int, string> _posterCache = new();

    public event EventHandler<MediaInfo>? MediaSelected;

    public SearchResultViewModel(ITmdbService tmdbService, ICacheService cacheService)
    {
        _tmdbService = tmdbService;
        _cacheService = cacheService;
    }

    public async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        SearchQuery = query;
        IsLoading = true;
        ErrorMessage = null;
        HasSearched = true;

        try
        {
            var results = await _tmdbService.SearchAsync(query);

            SearchResults.Clear();
            foreach (var result in results)
            {
                var cacheKey = _cacheService.GenerateCacheKey(result.TmdbId, result.MediaType);
                _cacheService.SaveToCache(cacheKey, result, 24);

                SearchResults.Add(result);
            }

            if (SearchResults.Count == 0)
            {
                ErrorMessage = "未找到匹配的影视作品";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"搜索失败: {ex.Message}";
            SearchResults.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<string?> GetPosterAsync(MediaInfo media)
    {
        if (media.PosterPath == null)
            return null;

        if (_posterCache.TryGetValue(media.TmdbId, out var cached))
            return cached;

        try
        {
            var base64 = await _tmdbService.DownloadImageAsync(media.PosterPath);
            if (!string.IsNullOrEmpty(base64))
            {
                var dataUrl = $"data:image/png;base64,{base64}";
                _posterCache[media.TmdbId] = dataUrl;
                return dataUrl;
            }
        }
        catch
        {
        }

        return null;
    }

    public void SelectMedia(MediaInfo media)
    {
        SelectedMedia = media;
        MediaSelected?.Invoke(this, media);
    }
}
