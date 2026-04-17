using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SmartFileMan.Plugins.MovieCollection.ViewModels;

namespace SmartFileMan.Plugins.MovieCollection.Views;

public partial class SearchResultPage : ContentView
{
    public SearchResultPage()
    {
        InitializeComponent();
        SearchEntry.Completed += OnSearchEntryCompleted;
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        await PerformSearch();
    }

    private async void OnSearchEntryCompleted(object? sender, EventArgs e)
    {
        await PerformSearch();
    }

    private async Task PerformSearch()
    {
        var viewModel = BindingContext as SearchResultViewModel;
        if (viewModel == null)
            return;

        var query = SearchEntry.Text;
        if (string.IsNullOrWhiteSpace(query))
            return;

        await viewModel.SearchAsync(query);
    }

    private void SelectMedia(object? sender, EventArgs e)
    {
        if (sender is not View view)
            return;

        var viewModel = BindingContext as SearchResultViewModel;
        if (viewModel == null)
            return;

        if (view.BindingContext is Models.MediaInfo media)
        {
            viewModel.SelectMedia(media);
        }
    }
}
