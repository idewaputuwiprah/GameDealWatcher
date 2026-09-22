using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardViewModel ViewModel { get; }
    private bool _initialized;

    public DashboardView(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ViewModel.InitializeAsync();
    }
}
