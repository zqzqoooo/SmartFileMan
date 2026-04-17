using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;
using SmartFileMan.Contracts.Core;

namespace SmartFileMan.Plugins.MovieCollection.ViewModels;

/// <summary>
/// 批量处理视图模型
/// Batch process view model
/// </summary>
public class BatchProcessViewModel : ViewModelBase
{
    private readonly IBatchModule _batchModule;
    private readonly ITmdbService _tmdbService;
    private readonly ICacheService _cacheService;
    private readonly IRenameService _renameService;

    private System.Collections.ObjectModel.ObservableCollection<MediaFileIndex> _files = new();
    public System.Collections.ObjectModel.ObservableCollection<MediaFileIndex> Files
    {
        get => _files;
        set => SetProperty(ref _files, value);
    }

    private MediaFileIndex? _selectedFile;
    public MediaFileIndex? SelectedFile
    {
        get => _selectedFile;
        set => SetProperty(ref _selectedFile, value);
    }

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    private MediaInfo? _matchedMedia;
    public MediaInfo? MatchedMedia
    {
        get => _matchedMedia;
        set => SetProperty(ref _matchedMedia, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<SeasonInfo> _seasons = new();
    public System.Collections.ObjectModel.ObservableCollection<SeasonInfo> Seasons
    {
        get => _seasons;
        set => SetProperty(ref _seasons, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> _episodes = new();
    public System.Collections.ObjectModel.ObservableCollection<EpisodeInfo> Episodes
    {
        get => _episodes;
        set => SetProperty(ref _episodes, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isAnalyzing;
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => SetProperty(ref _isAnalyzing, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private int _processedCount;
    public int ProcessedCount
    {
        get => _processedCount;
        set => SetProperty(ref _processedCount, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public BatchProcessViewModel(
        IBatchModule batchModule,
        ITmdbService tmdbService,
        ICacheService cacheService,
        IRenameService renameService)
    {
        _batchModule = batchModule;
        _tmdbService = tmdbService;
        _cacheService = cacheService;
        _renameService = renameService;
    }

    public async Task AnalyzeBatchAsync(BatchContext context)
    {
        IsAnalyzing = true;
        ErrorMessage = null;
        Files.Clear();

        try
        {
            var indices = await _batchModule.AnalyzeBatchAsync(context);

            foreach (var index in indices)
            {
                _cacheService.SaveFileIndex(index);
                Files.Add(index);
            }

            TotalCount = Files.Count;
            ProcessedCount = 0;

            if (Files.Count > 0)
            {
                SelectedFile = Files[0];
                await LoadMatchedMediaAsync(SelectedFile);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"分析失败: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    public async Task LoadMatchedMediaAsync(MediaFileIndex file)
    {
        if (file == null)
            return;

        IsLoading = true;
        MatchedMedia = null;
        Seasons.Clear();
        Episodes.Clear();

        try
        {
            var keyword = _batchModule.GenerateSearchKeyword(file.OriginalName);
            SearchKeyword = keyword;

            var results = await _tmdbService.SearchAsync(keyword, "tv");

            if (results.Count > 0)
            {
                MatchedMedia = results[0];
                var cacheKey = _cacheService.GenerateCacheKey(MatchedMedia.TmdbId, MatchedMedia.MediaType);
                _cacheService.SaveToCache(cacheKey, MatchedMedia, 24);

                await LoadSeasonsAsync(MatchedMedia.TmdbId);
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

    private async Task LoadSeasonsAsync(int tmdbId)
    {
        Seasons.Clear();

        for (int i = 1; i <= (MatchedMedia?.NumberOfSeasons ?? 0); i++)
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
    }

    public void SelectSeason(int seasonIndex)
    {
        if (seasonIndex < 0 || seasonIndex >= Seasons.Count)
            return;

        Episodes.Clear();
        var season = Seasons[seasonIndex];

        foreach (var episode in season.Episodes)
        {
            Episodes.Add(episode);
        }
    }

    public async Task RenameAllAsync()
    {
        if (MatchedMedia == null || Files.Count == 0)
            return;

        IsLoading = true;
        ProcessedCount = 0;

        try
        {
            var config = _cacheService.GetConfig();
            var proposals = _renameService.GenerateProposals(
                new List<MediaFileIndex>(Files),
                MatchedMedia,
                new List<SeasonInfo>(Seasons),
                config.RenameTemplate,
                config.TargetFolder);

            foreach (var proposal in proposals)
            {
                System.Diagnostics.Debug.WriteLine($"重命名: {proposal.OriginalName} -> {proposal.NewName}");
                ProcessedCount++;
            }

            ErrorMessage = $"成功处理 {ProcessedCount} 个文件";
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
}
