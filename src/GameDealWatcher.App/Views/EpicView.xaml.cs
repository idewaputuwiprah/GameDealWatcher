using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class EpicView : UserControl
{
    public EpicViewModel ViewModel { get; }
    private bool _initialized;

    public EpicView(EpicViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += EpicView_Loaded;
    }

    private async void EpicView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ViewModel.InitializeAsync();
    }
}
