using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.UI;
using SmartFileMan.Sdk;
using TagLib;

namespace SmartFileMan.Plugins.Music
{
    /// <summary>
    /// 音乐文件管理插件 - 按艺术家和专辑组织音乐文件
    /// Music File Management Plugin - Organizes music files by Artist and Album
    /// </summary>
    public class MusicPlugin : PluginBase, IFilePlugin, IPluginUI
    {
        public override string Id => "com.smartfileman.music";
        public override string DisplayName => "🎵 Music Librarian";
        public override string Description => "Organizes music files by Artist and Album.";
        public override PluginType Type => PluginType.Specific;

        private MusicLibraryView? _view;
        private MusicConfig? _config;

        private MusicConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Storage?.Load<MusicConfig>("Config") ?? new MusicConfig();
                    Storage?.Save("Config", _config);
                }
                return _config;
            }
        }

        public View GetView()
        {
            if (_view == null)
            {
                _view = new MusicLibraryView(Storage!, Config);
                _view.ConfigChanged += (sender, newConfig) =>
                {
                    _config = newConfig;
                    Storage?.Save("Config", newConfig);
                };
            }
            return _view;
        }

        public override async Task AnalyzeBatchAsync(BatchContext context)
        {
            var audioFiles = new List<IFileEntry>();
            var config = Config;

            foreach (var file in context.AllFiles)
            {
                if (IsAudioFile(file))
                {
                    audioFiles.Add(file);
                    await LoadMetadataAsync(file, config.ExtractAlbumArt);
                }
            }

            UpdateStatistics(audioFiles.Count, CountArtists(audioFiles), CountAlbums(audioFiles));
        }

        public override Task OnFileDetectedAsync(IFileEntry file)
        {
            return Task.CompletedTask;
        }

        public override async Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
        {
            if (!IsAudioFile(file))
                return null;

            var config = Config;

            if (!file.Properties.ContainsKey("Audio.Loaded"))
            {
                await LoadMetadataAsync(file, config.ExtractAlbumArt);
            }

            bool hasMetadata = file.Properties.ContainsKey("Audio.HasMetadata") && 
                               (bool)file.Properties["Audio.HasMetadata"];
            int score = hasMetadata ? 100 : 20;

            string artist = SanitizeFolderName(file.Properties.TryGetValue("Audio.Artist", out var a) && a is string astr ? a.ToString() ?? config.DefaultArtist : config.DefaultArtist);
            string album = SanitizeFolderName(file.Properties.TryGetValue("Audio.Album", out var al) && al is string alstr ? al.ToString() ?? config.DefaultAlbum : config.DefaultAlbum);
            string title = file.Properties.TryGetValue("Audio.Title", out var t) && t is string tstr && !string.IsNullOrWhiteSpace(tstr) ? tstr : Path.GetFileNameWithoutExtension(file.Name);
            string ext = file.Extension.ToLower();
            byte[]? albumArtData = file.Properties.TryGetValue("Audio.AlbumArtData", out var art) ? art as byte[] : null;

            string explanation = hasMetadata
                ? $"Music: {artist} - {title}"
                : "Music File (No Metadata)";

            string musicRoot = string.IsNullOrWhiteSpace(config.MusicFolder) 
                ? Path.Combine(file.DirectoryPath, "music") 
                : config.MusicFolder;

            if (!Path.IsPathRooted(musicRoot))
            {
                // Resolve relative path against the source file's directory
                musicRoot = Path.Combine(file.DirectoryPath, musicRoot);
            }
            musicRoot = Path.GetFullPath(musicRoot); // Normalize e.g. ./music/ to correct absolute path

            string destPath = config.FolderStructure switch
            {
                FolderStructure.Flat => Path.Combine(musicRoot, $"{artist} - {album}"),
                FolderStructure.Compact => Path.Combine(musicRoot, artist),
                _ => Path.Combine(musicRoot, artist, album)
            };

            var proposal = new RouteProposal(destPath, score, explanation)
            {
                OnProcessingSuccess = async (originalEntry, finalPath, hash) =>
                {
                    var track = new MusicTrack
                    {
                        Artist = artist,
                        Album = album,
                        Title = title,
                        Path = finalPath,
                        OriginalName = originalEntry.Name,
                        FileHash = hash,
                        Extension = ext,
                        AddedAt = DateTime.Now,
                        AlbumArtBase64 = (config.ExtractAlbumArt && albumArtData != null) 
                            ? Convert.ToBase64String(albumArtData) 
                            : null
                    };

                    SaveTrackToHistory(track);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _view?.AddTrack(track);
                    });

                    await Task.CompletedTask;
                }
            };

            return proposal;
        }

        private void SaveTrackToHistory(MusicTrack track)
        {
            try
            {
                var history = Storage?.Load<List<MusicTrack>>("History") ?? new List<MusicTrack>();
                
                history.RemoveAll(t => t.FileHash == track.FileHash || t.Path == track.Path);
                history.Insert(0, track);
                
                if (history.Count > 100)
                    history.RemoveAt(history.Count - 1);

                Storage?.Save("History", history);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MusicPlugin] Error saving track: {ex.Message}");
            }
        }

        private bool IsAudioFile(IFileEntry file)
        {
            var ext = file.Extension.ToLower();
            return ext == ".mp3" || ext == ".flac" || ext == ".m4a" || ext == ".wav" ||
                   ext == ".ogg" || ext == ".wma" || ext == ".aac" || ext == ".opus";
        }

        private Task LoadMetadataAsync(IFileEntry file, bool extractArt)
        {
            return Task.Run(() =>
            {
                try
                {
                    string? artist = null, album = null, title = null;
                    byte[]? albumArt = null;

                    using (var tfile = TagLib.File.Create(file.FullPath))
                    {
                        artist = tfile.Tag.FirstPerformer;
                        album = tfile.Tag.Album;
                        title = tfile.Tag.Title;

                        if (extractArt && tfile.Tag.Pictures.Length > 0)
                        {
                            var pic = tfile.Tag.Pictures[0];
                            albumArt = pic.Data.Data;
                            if (albumArt.Length > 5 * 1024 * 1024) // Allow up to 5MB album art
                                albumArt = null;
                        }
                    }

                    file.Properties["Audio.Artist"] = artist;
                    file.Properties["Audio.Album"] = album;
                    file.Properties["Audio.Title"] = title;
                    file.Properties["Audio.HasMetadata"] = !string.IsNullOrWhiteSpace(artist) ||
                                                            !string.IsNullOrWhiteSpace(album) ||
                                                            !string.IsNullOrWhiteSpace(title);
                    if (albumArt != null)
                        file.Properties["Audio.AlbumArtData"] = albumArt;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MusicPlugin] Error reading metadata: {ex.Message}");
                    file.Properties["Audio.HasMetadata"] = false;
                }
                finally
                {
                    file.Properties["Audio.Loaded"] = true;
                }
            });
        }

        private string SanitizeFolderName(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "Unknown";

            var result = input;
            foreach (char c in Path.GetInvalidFileNameChars())
                result = result.Replace(c, '_');

            return result.Length > 100 ? result.Substring(0, 100).Trim() : result.Trim();
        }

        private void UpdateStatistics(int totalTracks, int totalArtists, int totalAlbums)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _view?.UpdateStatistics(totalTracks, totalArtists, totalAlbums);
            });
        }

        private int CountArtists(List<IFileEntry> files)
        {
            var artists = new HashSet<string>();
            foreach (var file in files)
            {
                if (file.Properties["Audio.Artist"] is string artist && !string.IsNullOrEmpty(artist))
                    artists.Add(artist);
            }
            return artists.Count;
        }

        private int CountAlbums(List<IFileEntry> files)
        {
            var albums = new HashSet<string>();
            foreach (var file in files)
            {
                if (file.Properties["Audio.Album"] is string album && !string.IsNullOrEmpty(album))
                    albums.Add(album);
            }
            return albums.Count;
        }
    }

    public enum FolderStructure
    {
        ArtistAlbum = 0,
        Flat = 1,
        Compact = 2
    }

    public class MusicConfig
    {
        public string? MusicFolder { get; set; }
        public FolderStructure FolderStructure { get; set; } = FolderStructure.ArtistAlbum;
        public bool ExtractAlbumArt { get; set; } = true;
        public string DefaultArtist { get; set; } = "Unknown Artist";
        public string DefaultAlbum { get; set; } = "Unknown Album";
    }
}
