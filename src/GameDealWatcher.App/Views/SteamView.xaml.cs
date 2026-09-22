using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class SteamView : UserControl
{
    public SteamViewModel ViewModel { get; }
    private bool _initialized;

    public SteamView(SteamViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += SteamView_Loaded;
    }

    private async void SteamView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ViewModel.InitializeAsync();
    }
}
