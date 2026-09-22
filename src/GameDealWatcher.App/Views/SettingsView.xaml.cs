using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }
    private bool _initialized;

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ViewModel.InitializeAsync();
    }
}
