using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class SteamView : UserControl
{
    public SteamViewModel ViewModel { get; }

    public SteamView(SteamViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
