using GameDealWatcher.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using System.ComponentModel;
using WinRT.Interop;

namespace GameDealWatcher.App;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        
        Title = "GameDealWatcher";
        SizeWindow(1000, 600);
        
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentView))
        {
            ContentFrame.Content = ViewModel.CurrentView;
        }
    }

    private void SizeWindow(int widthDip, int heightDip)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        
        var scale = GetDpiForWindow(hwnd) / 96.0;
        appWindow.Resize(new Windows.Graphics.SizeInt32((int)(widthDip * scale), (int)(heightDip * scale)));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
}
