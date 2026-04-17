using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SmartFileMan.Contracts.Storage;
using System.Globalization;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;

namespace SmartFileMan.Plugins.Music
{
    public partial class MusicLibraryView : ContentView
    {
        public ObservableCollection<MusicTrack> Tracks { get; } = new();
        private readonly IPluginStorage _storage;
        private readonly MusicConfig _config;
        private string? _searchText;
        private bool _isSettingsVisible = false;
        private MusicTrack? _currentTrack;

        public event EventHandler<MusicConfig>? ConfigChanged;

        public MusicLibraryView(IPluginStorage storage, MusicConfig config)
        {
            InitializeComponent();
            _storage = storage;
            _config = config;
            MusicList.ItemsSource = Tracks;

            LoadHistory();
            UpdateSettingsUI();
        }

        private void LoadHistory()
        {
            try
            {
                var history = _storage.Load<System.Collections.Generic.List<MusicTrack>>("History");
                if (history != null)
                {
                    Tracks.Clear();
                    foreach (var track in history)
                    {
                        Tracks.Add(track);
                    }
                }
                UpdateStatisticsDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MusicLibrary] Error loading history: {ex.Message}");
            }
        }

        public void AddTrack(MusicTrack track)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Tracks.FirstOrDefault(t => t.FileHash == track.FileHash || t.Path == track.Path);
                if (existing != null)
                {
                    Tracks.Remove(existing);
                }

                Tracks.Insert(0, track);
                if (Tracks.Count > 100)
                {
                    Tracks.RemoveAt(Tracks.Count - 1);
                }

                UpdateStatisticsDisplay();
            });
        }

        public void UpdateStatistics(int totalTracks, int totalArtists, int totalAlbums)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatsTracks.Text = $"{totalTracks} Tracks";
                StatsArtists.Text = $"{totalArtists} Artists";
                StatsAlbums.Text = $"{totalAlbums} Albums";
            });
        }

        private void UpdateStatisticsDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatsTracks.Text = $"{Tracks.Count} Tracks";
                StatsArtists.Text = $"{Tracks.Select(t => t.Artist).Distinct().Count()} Artists";
                StatsAlbums.Text = $"{Tracks.Select(t => t.Album).Distinct().Count()} Albums";
            });
        }

        private void OnSearchClicked(object sender, EventArgs e)
        {
            _isSettingsVisible = false;
            SettingsSection.IsVisible = false;
            SearchSection.IsVisible = !SearchSection.IsVisible;
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            SearchSection.IsVisible = false;
            _isSettingsVisible = !_isSettingsVisible;
            SettingsSection.IsVisible = _isSettingsVisible;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue?.ToLower();
            FilterTracks();
        }

        private void FilterTracks()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                MusicList.ItemsSource = Tracks;
                return;
            }

            var filtered = Tracks.Where(t =>
                (t.Title?.ToLower().Contains(_searchText) ?? false) ||
                (t.Artist?.ToLower().Contains(_searchText) ?? false) ||
                (t.Album?.ToLower().Contains(_searchText) ?? false)
            ).ToList();

            MusicList.ItemsSource = new ObservableCollection<MusicTrack>(filtered);
        }

        private void OnPlayTrack(object sender, EventArgs e)
        {
            if (sender is Element element && element.BindingContext is MusicTrack track)
            {
                PlayTrack(track);
            }
        }

        private void PlayTrack(MusicTrack track)
        {
            _currentTrack = track;
            PlayerTitle.Text = track.Title;
            PlayerArtist.Text = track.Artist;

            UpdatePlayerArt(track);

            if (File.Exists(track.Path))
            {
                NowPlayingSection.IsVisible = true;

                try
                {
                    if (AudioPlayer != null)
                    {
                        AudioPlayer.Source = MediaSource.FromFile(track.Path);
                        AudioPlayer.Play();
                        PlayPauseButton.Text = "⏸";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MusicLibrary] AudioPlayer is null. MediaElement might not be initialized properly in the App.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MusicLibrary] Failed to play: {ex.Message}");
                }
            }
        }

        private void UpdatePlayerArt(MusicTrack track)
        {
            try
            {
                byte[]? imageData = GetAlbumArtData(track);
                if (imageData != null && imageData.Length > 0)
                {
                    PlayerArt.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
                }
                else
                {
                    PlayerArt.Source = null;
                }
            }
            catch
            {
                PlayerArt.Source = null;
            }
        }

        private static byte[]? GetAlbumArtData(MusicTrack track)
        {
            if (!string.IsNullOrEmpty(track.AlbumArtBase64))
            {
                try
                {
                    return Convert.FromBase64String(track.AlbumArtBase64);
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(track.Path) && File.Exists(track.Path))
            {
                try
                {
                    using var tfile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(track.Path), TagLib.ReadStyle.Average);
                    if (tfile.Tag.Pictures.Length > 0)
                    {
                        return tfile.Tag.Pictures[0].Data.Data;
                    }
                }
                catch { }
            }
            return null;
        }

        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (AudioPlayer.CurrentState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Playing)
            {
                AudioPlayer.Pause();
                PlayPauseButton.Text = "▶";
            }
            else
            {
                AudioPlayer.Play();
                PlayPauseButton.Text = "⏸";
            }
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            AudioPlayer.Stop();
            NowPlayingSection.IsVisible = false;
            _currentTrack = null;
            PlayPauseButton.Text = "▶";
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            PlayPauseButton.Text = "▶";
        }

        private async void OnSaveSettings(object sender, EventArgs e)
        {
            _config.MusicFolder = string.IsNullOrWhiteSpace(MusicFolderEntry.Text) ? null : MusicFolderEntry.Text;

            if (Enum.TryParse<FolderStructure>(FolderStructurePicker.SelectedItem?.ToString(), out var folderStructure))
            {
                _config.FolderStructure = folderStructure;
            }

            _config.ExtractAlbumArt = ExtractArtSwitch.IsToggled;
            _config.DefaultArtist = DefaultArtistEntry.Text ?? "Unknown Artist";
            _config.DefaultAlbum = DefaultAlbumEntry.Text ?? "Unknown Album";

            ConfigChanged?.Invoke(this, _config);
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlert("Settings Saved", "Your settings have been saved.", "OK");
            }
        }

        private void OnResetSettings(object sender, EventArgs e)
        {
            _config.MusicFolder = null;
            _config.FolderStructure = FolderStructure.ArtistAlbum;
            _config.ExtractAlbumArt = true;
            _config.DefaultArtist = "Unknown Artist";
            _config.DefaultAlbum = "Unknown Album";

            UpdateSettingsUI();
            ConfigChanged?.Invoke(this, _config);
        }

        private void UpdateSettingsUI()
        {
            MusicFolderEntry.Text = _config.MusicFolder ?? "";
            FolderStructurePicker.SelectedItem = _config.FolderStructure.ToString();
            ExtractArtSwitch.IsToggled = _config.ExtractAlbumArt;
            DefaultArtistEntry.Text = _config.DefaultArtist;
            DefaultAlbumEntry.Text = _config.DefaultAlbum;
        }

        private async void OnClearHistory(object sender, EventArgs e)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                bool confirm = await page.DisplayAlert("Clear History", "Are you sure you want to clear all music history?", "Yes", "No");
                if (confirm)
                {
                    Tracks.Clear();
                    _storage.Save("History", new System.Collections.Generic.List<MusicTrack>());
                    UpdateStatisticsDisplay();
                    SearchSection.IsVisible = false;
                }
            }
        }

        private async void OnExportPlaylist(object sender, EventArgs e)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;

            if (Tracks.Count == 0)
            {
                await page.DisplayAlert("Export", "No tracks to export.", "OK");
                return;
            }

            try
            {
                var playlistPath = Path.Combine(FileSystem.CacheDirectory, "playlist.m3u");
                using var writer = new StreamWriter(playlistPath);
                foreach (var track in Tracks)
                {
                    writer.WriteLine(track.Path);
                }
                await page.DisplayAlert("Export", $"Playlist exported to:\n{playlistPath}", "OK");
            }
            catch (Exception ex)
            {
                await page.DisplayAlert("Export Error", $"Failed to export playlist: {ex.Message}", "OK");
            }
        }
    }

    public class MusicTrack
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string Path { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string FileHash { get; set; } = "";
        public string Extension { get; set; } = "";
        public DateTime AddedAt { get; set; } = DateTime.Now;
        public string? AlbumArtBase64 { get; set; }
    }

    public class PathToAlbumArtConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MusicTrack track)
            {
                byte[]? imageData = null;

                if (!string.IsNullOrEmpty(track.AlbumArtBase64))
                {
                    try
                    {
                        imageData = System.Convert.FromBase64String(track.AlbumArtBase64);
                    }
                    catch { }
                }

                if (imageData == null && !string.IsNullOrEmpty(track.Path) && File.Exists(track.Path))
                {
                    try
                    {
                        using var tfile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(track.Path), TagLib.ReadStyle.Average);
                        if (tfile.Tag.Pictures.Length > 0)
                        {
                            imageData = tfile.Tag.Pictures[0].Data.Data;
                        }
                    }
                    catch { }
                }

                if (imageData != null && imageData.Length > 0)
                {
                    return ImageSource.FromStream(() => new MemoryStream(imageData));
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
