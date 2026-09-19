using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
