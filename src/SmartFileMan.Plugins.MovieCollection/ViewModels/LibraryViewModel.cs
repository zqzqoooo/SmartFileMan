using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;

namespace SmartFileMan.Plugins.MovieCollection.ViewModels;

public class LibraryItemViewModel : ViewModelBase
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterSource { get; set; } = string.Empty;
    public string MediaTypeLabel { get; set; } = string.Empty;
    public int EpisodeCount { get; set; }
}

public class LibraryViewModel : ViewModelBase
{
    private readonly ICacheService _cacheService;
    private readonly ITmdbService? _tmdbService;

    public ObservableCollection<LibraryItemViewModel> LibraryItems { get; } = new();

    private bool _hasNoVideos = true;

    private bool _isApiKeyMissing;
    public bool IsApiKeyMissing
    {
        get => _isApiKeyMissing;
        set 
        {
            if (SetProperty(ref _isApiKeyMissing, value))
            {
                OnPropertyChanged(nameof(DisplayPosterWall));
            }
        }
    }

    public bool HasNoVideos
    {
        get => _hasNoVideos;
        set 
        {
            if (SetProperty(ref _hasNoVideos, value))
            {
                OnPropertyChanged(nameof(DisplayPosterWall));
            }
        }
    }

    public bool DisplayPosterWall => !HasNoVideos && !IsApiKeyMissing;

    public ICommand ItemSelectedCommand { get; }
    public EventHandler<LibraryItemViewModel>? LibraryItemSelected;

    public LibraryViewModel(ICacheService cacheService, ITmdbService? tmdbService)
    {
        _cacheService = cacheService;
        _tmdbService = tmdbService;
        
        ItemSelectedCommand = new Command<LibraryItemViewModel>(OnItemSelected);
    }

    public void LoadLibrary()
    {
        var config = _cacheService.GetConfig();
        if (string.IsNullOrEmpty(config.TmdbApiKey))
        {
            IsApiKeyMissing = true;
            HasNoVideos = false;
            LibraryItems.Clear();
            return;
        }

        IsApiKeyMissing = false;

        var allIndices = _cacheService.GetAllFileIndices();
        var validIndices = allIndices.Where(i => i.TmdbId.HasValue).ToList();

        if (!validIndices.Any())
        {
            HasNoVideos = true;
            LibraryItems.Clear();
            return;
        }

        HasNoVideos = false;
        LibraryItems.Clear();

        var grouped = validIndices.GroupBy(i => new { i.TmdbId, i.MediaType });

        foreach (var group in grouped)
        {
            var tmdbId = group.Key.TmdbId!.Value;
            var mediaType = group.Key.MediaType;
            
            // Try to find cached media info
            var cacheKey = _cacheService.GenerateCacheKey(tmdbId, mediaType);
            var cachedInfo = _cacheService.GetCached(cacheKey);

            var item = new LibraryItemViewModel
            {
                TmdbId = tmdbId,
                Title = cachedInfo?.Title ?? $"TMDB 影片 ID {tmdbId}", // Fallback title
                PosterSource = !string.IsNullOrEmpty(cachedInfo?.PosterPath) ? $"https://image.tmdb.org/t/p/w500{cachedInfo.PosterPath}" : "dotnet_bot.png",
                MediaTypeLabel = mediaType == "tv" ? "电视剧" : "电影",
                EpisodeCount = group.Count()
            };

            LibraryItems.Add(item);
        }
    }

    private void OnItemSelected(LibraryItemViewModel? item)
    {
        if (item != null)
        {
            LibraryItemSelected?.Invoke(this, item);
        }
    }
}