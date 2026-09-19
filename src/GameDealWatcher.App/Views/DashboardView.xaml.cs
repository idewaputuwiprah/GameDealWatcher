using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardViewModel ViewModel { get; }

    public DashboardView(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
