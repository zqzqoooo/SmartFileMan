using System;
using System.Collections.Generic;
using System.Windows.Input;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;

namespace SmartFileMan.Plugins.MovieCollection.ViewModels;

/// <summary>
/// 详情页视图模型
/// Detail page view model
/// </summary>
public class DetailPageViewModel : ViewModelBase
{
    private readonly ITmdbService _tmdbService;
    private readonly ICacheService _cacheService;
    private readonly IRenameService _renameService;

    private MediaInfo? _mediaInfo;
    public MediaInfo? MediaInfo
    {
        get => _mediaInfo;
        set => SetProperty(ref _mediaInfo, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<SeasonInfo> _seasons = new();
    public System.Collections.ObjectModel.ObservableCollection<SeasonInfo> Seasons
    {
        get => _seasons;
        set => SetProperty(ref _seasons, value);
    }

    private SeasonInfo? _selectedSeason;
    public SeasonInfo? SelectedSeason
    {
        get => _selectedSeason;
        set => SetProperty(ref _selectedSeason, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> _episodes = new();
    public System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> Episodes
    {
        get => _episodes;
        set => SetProperty(ref _episodes, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<MediaFileIndex> _files = new();
    public System.Collections.ObjectModel.ObservableCollection<MediaFileIndex> Files
    {
        get => _files;
        set => SetProperty(ref _files, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> _selectedEpisodes = new();
    public System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> SelectedEpisodes
    {
        get => _selectedEpisodes;
        set => SetProperty(ref _selectedEpisodes, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _posterDataUrl = string.Empty;
    public string PosterDataUrl
    {
        get => _posterDataUrl;
        set => SetProperty(ref _posterDataUrl, value);
    }

    public ICommand LoadDetailsCommand { get; }
    public ICommand LoadSeasonsCommand { get; }
    public ICommand SelectAllEpisodesCommand { get; }
    public ICommand DeselectAllEpisodesCommand { get; }
    public ICommand RenameSelectedCommand { get; }
    public ICommand DownloadPosterCommand { get; }
    public ICommand GoBackCommand { get; }

    public Action? OnGoBack;

    public DetailPageViewModel(ITmdbService tmdbService, ICacheService cacheService, IRenameService renameService)
    {
        _tmdbService = tmdbService;
        _cacheService = cacheService;
        _renameService = renameService;

        LoadDetailsCommand = new Command<int>(async (tmdbId) => await LoadDetailsAsync(tmdbId));
        LoadSeasonsCommand = new Command<int>(async (tmdbId) => await LoadSeasonsAsync(tmdbId));
        SelectAllEpisodesCommand = new Command(SelectAllEpisodes);
        DeselectAllEpisodesCommand = new Command(DeselectAllEpisodes);
        RenameSelectedCommand = new Command(async () => await RenameSelectedEpisodesAsync(), () => SelectedEpisodes.Count > 0);
        DownloadPosterCommand = new Command(async () => await DownloadPosterAsync());
        GoBackCommand = new Command(() => OnGoBack?.Invoke());
    }

    public async Task LoadDetailsAsync(int tmdbId)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(tmdbId, "tv");
            var cached = _cacheService.GetCached(cacheKey);

            if (cached != null)
            {
                MediaInfo = cached;
                await LoadSeasonsAsync(tmdbId);
                return;
            }

            MediaInfo = await _tmdbService.GetTvDetailsAsync(tmdbId);

            if (MediaInfo != null)
            {
                _cacheService.SaveToCache(cacheKey, MediaInfo);
                await LoadSeasonsAsync(tmdbId);
            }
            else
            {
                ErrorMessage = "无法获取影视详情，请检查网络连接";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadSeasonsAsync(int tmdbId)
    {
        if (MediaInfo == null || !MediaInfo.NumberOfSeasons.HasValue)
            return;

        Seasons.Clear();

        for (int i = 1; i <= MediaInfo.NumberOfSeasons.Value; i++)
        {
            try
            {
                var season = await _tmdbService.GetSeasonDetailsAsync(tmdbId, i);
                if (season != null)
                {
                    Seasons.Add(season);
                }
            }
            catch
            {
            }
        }

        if (Seasons.Count > 0)
        {
            SelectedSeason = Seasons[0];
        }
    }

    public void LoadEpisodesForSeason(int seasonIndex)
    {
        if (seasonIndex < 0 || seasonIndex >= Seasons.Count)
            return;

        SelectedSeason = Seasons[seasonIndex];
        Episodes.Clear();

        if (SelectedSeason?.Episodes != null)
        {
            foreach (var episode in SelectedSeason.Episodes)
            {
                Episodes.Add(episode);
            }
        }
    }

    private void SelectAllEpisodes()
    {
        SelectedEpisodes.Clear();
        foreach (var episode in Episodes)
        {
            SelectedEpisodes.Add(episode);
        }
        OnPropertyChanged(nameof(SelectedEpisodes));
    }

    private void DeselectAllEpisodes()
    {
        SelectedEpisodes.Clear();
        OnPropertyChanged(nameof(SelectedEpisodes));
    }

    public async Task RenameSelectedEpisodesAsync()
    {
        if (MediaInfo == null || SelectedEpisodes.Count == 0)
            return;

        IsLoading = true;

        try
        {
            var filesToRename = new List<MediaFileIndex>();

            foreach (var episode in SelectedEpisodes)
            {
                var fileIndex = Files.FirstOrDefault(f =>
                    f.SeasonNumber == episode.SeasonNumber &&
                    f.EpisodeNumber == episode.EpisodeNumber);

                if (fileIndex != null)
                {
                    filesToRename.Add(fileIndex);
                }
            }

            var config = _cacheService.GetConfig();
            var proposals = _renameService.GenerateProposals(
                filesToRename,
                MediaInfo,
                new List<SeasonInfo>(Seasons),
                config.RenameTemplate,
                config.TargetFolder);

            foreach (var proposal in proposals)
            {
                System.Diagnostics.Debug.WriteLine($"重命名: {proposal.OriginalName} -> {proposal.NewName}");
            }

            SelectedEpisodes.Clear();
            OnPropertyChanged(nameof(SelectedEpisodes));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"重命名失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DownloadPosterAsync()
    {
        if (MediaInfo?.PosterPath == null)
            return;

        try
        {
            var base64 = await _tmdbService.DownloadImageAsync(MediaInfo.PosterPath);
            if (!string.IsNullOrEmpty(base64))
            {
                PosterDataUrl = $"data:image/png;base64,{base64}";
            }
        }
        catch
        {
        }
    }
}
