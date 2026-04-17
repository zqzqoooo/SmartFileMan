using System.IO;
using System.Text.RegularExpressions;
using SmartFileMan.Plugins.MovieCollection.Models;

namespace SmartFileMan.Plugins.MovieCollection.Services;

/// <summary>
/// 重命名服务实现
/// Rename service implementation for media files
/// </summary>
public class RenameService : IRenameService
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// 从文件名中提取集数的正则表达式
    /// Regex patterns for extracting episode information
    /// </summary>
    private static readonly Regex[] EpisodePatterns = new[]
    {
        new Regex(@"S(\d{1,2})E(\d{1,2})", RegexOptions.IgnoreCase),
        new Regex(@"(\d{1,2})x(\d{1,2})", RegexOptions.IgnoreCase),
        new Regex(@"第(\d+)季.*?第(\d+)集", RegexOptions.IgnoreCase),
        new Regex(@"\[(\d{1,2})\].*?\[(\d{1,2})\]", RegexOptions.IgnoreCase),
    };

    /// <inheritdoc />
    public List<RenameProposal> GenerateProposals(
        List<MediaFileIndex> files,
        MediaInfo mediaInfo,
        List<SeasonInfo> seasons,
        string template,
        string? targetFolder = null)
    {
        var proposals = new List<RenameProposal>();
        var baseDirectory = string.IsNullOrEmpty(targetFolder)
            ? Path.GetDirectoryName(files.FirstOrDefault()?.OriginalPath) ?? ""
            : targetFolder;

        foreach (var file in files.Where(f => f.SeasonNumber.HasValue && f.EpisodeNumber.HasValue))
        {
            var season = seasons.FirstOrDefault(s => s.SeasonNumber == file.SeasonNumber);
            var episode = season?.Episodes.FirstOrDefault(e => e.EpisodeNumber == file.EpisodeNumber);

            if (episode == null)
            {
                episode = new EpisodeInfo
                {
                    SeasonNumber = file.SeasonNumber!.Value,
                    EpisodeNumber = file.EpisodeNumber!.Value,
                    Name = $"Episode {file.EpisodeNumber}"
                };
            }

            var newName = FormatEpisodeTitle(template, mediaInfo, episode);
            newName = SanitizeFileName(newName);
            var extension = GetExtension(file.OriginalPath);
            newName = $"{newName}{extension}";

            var newPath = Path.Combine(baseDirectory, newName);

            proposals.Add(new RenameProposal
            {
                OriginalPath = file.OriginalPath,
                OriginalName = file.OriginalName,
                NewName = newName,
                NewPath = newPath,
                MediaInfo = mediaInfo,
                Score = 95,
                Reason = $"匹配到 {mediaInfo.Title} 第{file.SeasonNumber}季第{file.EpisodeNumber}集"
            });
        }

        return proposals.OrderBy(p => p.Score).ToList();
    }

    /// <inheritdoc />
    public string FormatEpisodeTitle(string template, MediaInfo mediaInfo, EpisodeInfo episode)
    {
        return template
            .Replace("{title}", mediaInfo.Title)
            .Replace("{season:00}", episode.SeasonNumber.ToString("00"))
            .Replace("{episode:00}", episode.EpisodeNumber.ToString("00"))
            .Replace("{season}", episode.SeasonNumber.ToString())
            .Replace("{episode}", episode.EpisodeNumber.ToString())
            .Replace("{episode_name}", episode.Name)
            .Replace("{original_title}", mediaInfo.OriginalTitle);
    }

    /// <inheritdoc />
    public string GetExtension(string originalPath)
    {
        return Path.GetExtension(originalPath);
    }

    /// <inheritdoc />
    public bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return !fileName.Any(c => InvalidFileNameChars.Contains(c));
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// Sanitize filename by removing invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in InvalidFileNameChars)
        {
            fileName = fileName.Replace(c.ToString(), "_");
        }

        fileName = fileName.Replace("..", "_");
        fileName = Regex.Replace(fileName, @"\s+", " ");
        fileName = fileName.Trim();

        return fileName;
    }
}
