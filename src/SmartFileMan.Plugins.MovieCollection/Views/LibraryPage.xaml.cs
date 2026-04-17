using Microsoft.Maui.Controls;
using SmartFileMan.Plugins.MovieCollection.ViewModels;

namespace SmartFileMan.Plugins.MovieCollection.Views;

public partial class LibraryPage : ContentView
{
    public LibraryPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is LibraryViewModel vm)
        {
            vm.LoadLibrary();
        }
    }
}
