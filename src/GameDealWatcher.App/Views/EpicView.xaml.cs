using GameDealWatcher.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GameDealWatcher.App.Views;

public partial class EpicView : UserControl
{
    public EpicViewModel ViewModel { get; }

    public EpicView(EpicViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
