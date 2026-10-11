using System.Globalization;
using System.IO;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Infrastructure.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameDealWatcher.App.Views;

public sealed partial class GameCard : UserControl
{
    public static readonly DependencyProperty DealProperty = DependencyProperty.Register(
        nameof(Deal), typeof(GameDeal), typeof(GameCard), new PropertyMetadata(null, OnDealChanged));

    public GameDeal? Deal
    {
        get => (GameDeal?)GetValue(DealProperty);
        set => SetValue(DealProperty, value);
    }

    private CancellationTokenSource? _imageCts;

    public GameCard()
    {
        InitializeComponent();
        Unloaded += (s, e) =>
        {
            try { _imageCts?.Cancel(); }
            catch (ObjectDisposedException) { }
            _imageCts?.Dispose();
        };
    }

    private static void OnDealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GameCard)d).LoadCoverImageAsync();

    private async void LoadCoverImageAsync()
    {
        try
        {
            _imageCts?.Cancel();
            _imageCts?.Dispose();
        }
        finally
        {
            _imageCts = new CancellationTokenSource();
        }
        var ct = _imageCts.Token;

        var url = Deal?.ImageUrl;
        CoverImage.Source = null;
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            var cache = App.Services?.GetService<IImageCacheService>();
            var localPath = cache != null
                ? await cache.GetOrDownloadImageAsync(url, ct)
                : null;
            if (ct.IsCancellationRequested) return;

            if (localPath != null)
            {
                // Decode from an in-memory copy rather than binding the file URI,
                // so the cache file isn't held open while displayed — "Clear Image
                // Cache" must be able to delete it.
                var bytes = await File.ReadAllBytesAsync(localPath, ct);
                if (ct.IsCancellationRequested) return;
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(bytes).AsRandomAccessStream();
                await bitmap.SetSourceAsync(stream);
                if (ct.IsCancellationRequested) return;
                CoverImage.Source = bitmap;
            }
            else if (Uri.TryCreate(url, UriKind.Absolute, out var remote))
            {
                CoverImage.Source = new BitmapImage(remote);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!ct.IsCancellationRequested && Uri.TryCreate(url, UriKind.Absolute, out var remote))
                CoverImage.Source = new BitmapImage(remote);
        }
    }

    private async void OpenStore_Click(object sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(Deal?.StoreUrl, UriKind.Absolute, out var uri))
            await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    // x:Bind helpers — must tolerate a null deal before the card is bound.
    public string DiscountText(GameDeal? deal) =>
        deal is { DiscountPercentage: > 0 } ? $"-{deal.DiscountPercentage}%" : string.Empty;

    public Visibility DiscountBadgeVisibility(GameDeal? deal) =>
        deal is { DiscountPercentage: > 0, IsCurrentlyFree: false, IsUpcoming: false } ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FreeBadgeVisibility(GameDeal? deal) =>
        deal?.IsCurrentlyFree == true ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UpcomingBadgeVisibility(GameDeal? deal) =>
        deal?.IsUpcoming == true ? Visibility.Visible : Visibility.Collapsed;

    public string CurrentPriceText(GameDeal? deal) =>
        deal == null ? string.Empty : FormatPrice(deal.CurrentPrice);

    public string OriginalPriceText(GameDeal? deal) =>
        deal == null ? string.Empty : FormatPrice(deal.OriginalPrice);

    public Visibility OriginalPriceVisibility(GameDeal? deal) =>
        deal is { DiscountPercentage: > 0 } ? Visibility.Visible : Visibility.Collapsed;

    public string DateRangeText(GameDeal? deal) => deal switch
    {
        { IsUpcoming: true, StartsAt: { } starts } => $"Starts {starts.LocalDateTime:MMM d}",
        { EndsAt: { } ends } => $"Until {ends.LocalDateTime:MMM d}",
        _ => string.Empty
    };

    public Visibility DateRangeVisibility(GameDeal? deal) =>
        DateRangeText(deal).Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    public string OpenStoreAutomationName(GameDeal? deal) =>
        deal != null ? $"Open {deal.Title} in store" : "Open Store";

    private static string FormatPrice(decimal price) =>
        price <= 0 ? "Free" : price.ToString("$0.00", CultureInfo.InvariantCulture);
}
