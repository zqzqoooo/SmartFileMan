using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 批次分析模块
/// Batch analysis module for media file processing
/// </summary>
public class BatchModule : IBatchModule
{
    /// <summary>
    /// 支持的视频文件扩展名
    /// Supported video file extensions
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".flv", ".webm"
    };

    /// <summary>
    /// 从文件名提取集数的正则表达式
    /// Regex patterns for extracting episode information
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex[] EpisodePatterns = new[]
    {
        new System.Text.RegularExpressions.Regex(@"S(\d{1,2})E(\d{1,2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        new System.Text.RegularExpressions.Regex(@"(\d{1,2})x(\d{1,2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        new System.Text.RegularExpressions.Regex(@"第(\d+)季.*?第(\d+)集", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        new System.Text.RegularExpressions.Regex(@"\[(\d{1,2})\].*?\[(\d{1,2})\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        new System.Text.RegularExpressions.Regex(@"第(\d+)集", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// 电视剧常见关键词
    /// Common TV show keywords
    /// </summary>
    private static readonly HashSet<string> TvShowKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "season", "episode", "ep", "s01", "s02", "s03", "s04", "s05",
        "第", "季", "集"
    };

    /// <inheritdoc />
    public async Task<List<MediaFileIndex>> AnalyzeBatchAsync(BatchContext context)
    {
        var indices = new List<MediaFileIndex>();

        foreach (var file in context.AllFiles)
        {
            if (!IsVideoFile(file.Extension))
                continue;

            var index = new MediaFileIndex
            {
                OriginalPath = file.FullPath,
                OriginalName = file.Name,
                MediaType = RecognizeMediaType(file.Name)
            };

            var episodeInfo = ExtractEpisodeInfo(file.Name);
            if (episodeInfo != null)
            {
                index.SeasonNumber = episodeInfo.SeasonNumber;
                index.EpisodeNumber = episodeInfo.EpisodeNumber;
                index.Status = ProcessingStatus.Recognized;
            }

            if (file.GetHashAsync != null)
            {
                try
                {
                    index.FileHash = await file.GetHashAsync();
                }
                catch
                {
                }
            }

            indices.Add(index);
        }

        return indices;
    }

    /// <inheritdoc />
    public EpisodeInfo? ExtractEpisodeInfo(string fileName)
    {
        foreach (var pattern in EpisodePatterns)
        {
            var match = pattern.Match(fileName);
            if (match.Success)
            {
                if (match.Groups.Count >= 3)
                {
                    if (int.TryParse(match.Groups[1].Value, out int seasonOrEpisode) &&
                        int.TryParse(match.Groups[2].Value, out int episode))
                    {
                        var isTvPattern = pattern.ToString().Contains("S") || 
                                          pattern.ToString().Contains("x") ||
                                          pattern.ToString().Contains("第");

                        if (isTvPattern && seasonOrEpisode <= 30 && episode <= 100)
                        {
                            return new EpisodeInfo
                            {
                                SeasonNumber = seasonOrEpisode,
                                EpisodeNumber = episode
                            };
                        }
                    }
                }
                else if (match.Groups.Count >= 2)
                {
                    if (int.TryParse(match.Groups[1].Value, out int episode))
                    {
                        return new EpisodeInfo
                        {
                            SeasonNumber = 1,
                            EpisodeNumber = episode
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string RecognizeMediaType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unknown";

        var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var hasSeasonInfo = EpisodePatterns.Any(p => p.IsMatch(nameWithoutExtension));
        if (hasSeasonInfo)
            return "tv";

        var lowerName = nameWithoutExtension.ToLowerInvariant();
        if (TvShowKeywords.Any(keyword => lowerName.Contains(keyword)))
            return "tv";

        return "movie";
    }

    /// <inheritdoc />
    public string GenerateSearchKeyword(string fileName)
    {
        var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"S\d{1,2}E\d{1,2}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"\d{1,2}x\d{1,2}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"第\d+季.*?第\d+集", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"\[.*?\].*?\[.*?\]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"(?<![0-9])\d{4}(?![0-9a-zA-Z])", "");
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"\d{1,2} episodes?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"(720p|1080p|2160p|4k|uhd|hdr|bluray|webrip|web-dl|dvdrip|x264|x265|hevc|aac|ac3|dts)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"\.{2,}", ".", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[._-]+", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"\s+", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        nameWithoutExtension = nameWithoutExtension.Trim(' ', '.');
        nameWithoutExtension = nameWithoutExtension.Trim();

        return nameWithoutExtension;
    }

    /// <summary>
    /// 判断是否为视频文件
    /// Check if file extension is a supported video format
    /// </summary>
    private static bool IsVideoFile(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        return VideoExtensions.Contains(extension);
    }
}
