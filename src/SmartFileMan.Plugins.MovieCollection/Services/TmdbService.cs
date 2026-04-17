using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SmartFileMan.Plugins.MovieCollection.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// TMDB API 服务实现
/// TMDB API service implementation
/// </summary>
public class TmdbService : ITmdbService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";
    private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p";
    private bool _disposed;

    public TmdbService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private Dictionary<string, string> GetCommonParams() => new()
    {
        ["api_key"] = _apiKey,
        ["language"] = "zh-CN"
    };

    private static string AddQueryParams(string url, Dictionary<string, string> queryParams)
    {
        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"{url}?{queryString}";
    }

    /// <inheritdoc />
    public async Task<List<MediaInfo>> SearchAsync(string query, string mediaType = "all")
    {
        var results = new List<MediaInfo>();
        var url = $"{_baseUrl}/search/multi";

        var queryParams = GetCommonParams();
        queryParams["query"] = query;
        queryParams["include_adult"] = "false";

        var response = await _httpClient.GetAsync(AddQueryParams(url, queryParams));
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var element in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            var type = element.GetProperty("media_type").GetString();
            if (type == "movie" || type == "tv")
            {
                results.Add(ParseMediaInfo(element, type!));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<MediaInfo?> GetMovieDetailsAsync(int tmdbId)
    {
        var url = $"{_baseUrl}/movie/{tmdbId}";
        var queryParams = GetCommonParams();
        queryParams["append_to_response"] = "credits";

        var response = await _httpClient.GetAsync(AddQueryParams(url, queryParams));
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return ParseMovieDetails(doc.RootElement);
    }

    /// <inheritdoc />
    public async Task<MediaInfo?> GetTvDetailsAsync(int tmdbId)
    {
        var url = $"{_baseUrl}/tv/{tmdbId}";
        var queryParams = GetCommonParams();
        queryParams["append_to_response"] = "credits";

        var response = await _httpClient.GetAsync(AddQueryParams(url, queryParams));
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return ParseTvDetails(doc.RootElement);
    }

    /// <inheritdoc />
    public async Task<SeasonInfo?> GetSeasonDetailsAsync(int tmdbId, int seasonNumber)
    {
        var url = $"{_baseUrl}/tv/{tmdbId}/season/{seasonNumber}";
        var queryParams = GetCommonParams();

        var response = await _httpClient.GetAsync(AddQueryParams(url, queryParams));
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return ParseSeasonInfo(doc.RootElement);
    }

    /// <inheritdoc />
    public async Task<string?> DownloadImageAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        try
        {
            var imageUrl = $"{_imageBaseUrl}/w500{path}";
            var response = await _httpClient.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static MediaInfo ParseMediaInfo(JsonElement element, string mediaType)
    {
        var mediaInfo = new MediaInfo
        {
            TmdbId = element.GetProperty("id").GetInt32(),
            MediaType = mediaType,
            Title = element.TryGetProperty("title", out var title) 
                ? title.GetString() ?? "" 
                : element.GetProperty("name").GetString() ?? "",
            OriginalTitle = element.TryGetProperty("original_title", out var ot) 
                ? ot.GetString() ?? "" 
                : element.GetProperty("original_name").GetString() ?? "",
            Overview = element.TryGetProperty("overview", out var overview) 
                ? overview.GetString() ?? "" 
                : "",
            VoteAverage = element.TryGetProperty("vote_average", out var vote) 
                ? vote.GetDouble() 
                : 0,
            VoteCount = element.TryGetProperty("vote_count", out var vc) 
                ? vc.GetInt32() 
                : 0
        };

        if (element.TryGetProperty("poster_path", out var poster) && poster.ValueKind != JsonValueKind.Null)
        {
            mediaInfo.PosterPath = poster.GetString();
        }

        if (element.TryGetProperty("backdrop_path", out var backdrop) && backdrop.ValueKind != JsonValueKind.Null)
        {
            mediaInfo.BackdropPath = backdrop.GetString();
        }

        if (element.TryGetProperty("release_date", out var releaseDate))
        {
            if (DateTime.TryParse(releaseDate.GetString(), out var date))
                mediaInfo.ReleaseDate = date;
        }
        else if (element.TryGetProperty("first_air_date", out var firstAirDate))
        {
            if (DateTime.TryParse(firstAirDate.GetString(), out var date))
                mediaInfo.ReleaseDate = date;
        }

        return mediaInfo;
    }

    private static MediaInfo ParseMovieDetails(JsonElement element)
    {
        var mediaInfo = ParseMediaInfo(element, "movie");

        if (element.TryGetProperty("runtime", out var runtime) && runtime.ValueKind == JsonValueKind.Number)
        {
            mediaInfo.Runtime = runtime.GetInt32();
        }

        if (element.TryGetProperty("genres", out var genres))
        {
            mediaInfo.Genres = genres.EnumerateArray()
                .Select(g => g.GetProperty("name").GetString() ?? "")
                .ToList();
        }

        if (element.TryGetProperty("production_countries", out var countries))
        {
            mediaInfo.ProductionCountries = countries.EnumerateArray()
                .Select(c => c.GetProperty("name").GetString() ?? "")
                .ToList();
        }

        if (element.TryGetProperty("credits", out var credits))
        {
            ParseCredits(credits, mediaInfo);
        }

        return mediaInfo;
    }

    private static MediaInfo ParseTvDetails(JsonElement element)
    {
        var mediaInfo = ParseMediaInfo(element, "tv");

        if (element.TryGetProperty("number_of_seasons", out var seasons) && seasons.ValueKind == JsonValueKind.Number)
        {
            mediaInfo.NumberOfSeasons = seasons.GetInt32();
        }

        if (element.TryGetProperty("number_of_episodes", out var episodes) && episodes.ValueKind == JsonValueKind.Number)
        {
            mediaInfo.NumberOfEpisodes = episodes.GetInt32();
        }

        if (element.TryGetProperty("genres", out var genres))
        {
            mediaInfo.Genres = genres.EnumerateArray()
                .Select(g => g.GetProperty("name").GetString() ?? "")
                .ToList();
        }

        if (element.TryGetProperty("production_countries", out var countries))
        {
            mediaInfo.ProductionCountries = countries.EnumerateArray()
                .Select(c => c.GetProperty("name").GetString() ?? "")
                .ToList();
        }

        if (element.TryGetProperty("episode_run_time", out var runTime))
        {
            var runTimeArray = runTime.EnumerateArray().ToList();
            if (runTimeArray.Count > 0)
            {
                mediaInfo.Runtime = runTimeArray.First().GetInt32();
            }
        }

        if (element.TryGetProperty("credits", out var credits))
        {
            ParseCredits(credits, mediaInfo);
        }

        return mediaInfo;
    }

    private static void ParseCredits(JsonElement credits, MediaInfo mediaInfo)
    {
        if (credits.TryGetProperty("cast", out var cast))
        {
            mediaInfo.Cast = cast.EnumerateArray()
                .Take(10)
                .Select(c => new CastMember
                {
                    Id = c.GetProperty("id").GetInt32(),
                    Name = c.GetProperty("name").GetString() ?? "",
                    Character = c.TryGetProperty("character", out var charProp) ? charProp.GetString() ?? "" : "",
                    ProfilePath = c.TryGetProperty("profile_path", out var profile) && profile.ValueKind != JsonValueKind.Null
                        ? profile.GetString()
                        : null
                })
                .ToList();
        }

        if (credits.TryGetProperty("crew", out var crew))
        {
            mediaInfo.Directors = crew.EnumerateArray()
                .Where(c => c.TryGetProperty("job", out var job) && job.GetString() == "Director")
                .Select(d => d.GetProperty("name").GetString() ?? "")
                .ToList();
        }
    }

    private static SeasonInfo ParseSeasonInfo(JsonElement element)
    {
        var seasonInfo = new SeasonInfo
        {
            SeasonNumber = element.GetProperty("season_number").GetInt32(),
            Name = element.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            Overview = element.TryGetProperty("overview", out var overview) ? overview.GetString() ?? "" : ""
        };

        if (element.TryGetProperty("poster_path", out var poster) && poster.ValueKind != JsonValueKind.Null)
        {
            seasonInfo.PosterPath = poster.GetString();
        }

        if (element.TryGetProperty("episodes", out var episodes))
        {
            seasonInfo.Episodes = episodes.EnumerateArray()
                .Select(e => new EpisodeInfo
                {
                    SeasonNumber = seasonInfo.SeasonNumber,
                    EpisodeNumber = e.GetProperty("episode_number").GetInt32(),
                    Name = e.TryGetProperty("name", out var epName) ? epName.GetString() ?? "" : "",
                    Overview = e.TryGetProperty("overview", out var epOverview) ? epOverview.GetString() ?? "" : "",
                    StillPath = e.TryGetProperty("still_path", out var still) && still.ValueKind != JsonValueKind.Null
                        ? still.GetString()
                        : null,
                    AirDate = e.TryGetProperty("air_date", out var airDate) && DateTime.TryParse(airDate.GetString(), out var date)
                        ? date
                        : null
                })
                .ToList();

            seasonInfo.EpisodeCount = seasonInfo.Episodes.Count;
        }

        return seasonInfo;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
