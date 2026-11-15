using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Config;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class CompareBar : UserControl
{
    private DisposableBag _subscriptions;

    public CompareBar()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += (_, _) => _subscriptions.Dispose();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _subscriptions.Dispose();
        _subscriptions = default;

        if (DataContext is not TabViewModel tab)
        {
            IsVisible = false;
            return;
        }

        tab.ParentWindowContext.GlobalSettings.IsShowingPair
            .Subscribe(visible => IsVisible = visible)
            .AddTo(ref _subscriptions);

        tab.Zoom.Level
            .Subscribe(level => ZoomPercentText.Text = $"{level:0}%")
            .AddTo(ref _subscriptions);
    }

    private GlobalSettingsViewModel? SettingsVm =>
        (DataContext as TabViewModel)?.ParentWindowContext.GlobalSettings;

    private void SetStackedMode(ComparisonMode mode)
    {
        if (SettingsVm is not { } gs)
        {
            return;
        }

        gs.ComparisonMode.Value = mode;
        gs.IsComparing.Value = true;
    }

    private void OnOverlayModeClick(object? sender, RoutedEventArgs e) =>
        SetStackedMode(ComparisonMode.Overlay);

    private void OnSplitModeClick(object? sender, RoutedEventArgs e) =>
        SetStackedMode(ComparisonMode.Split);

    private void OnSideBySideModeClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsSideBySide.Value = true;
        }
    }

    private void OnBlinkModeClick(object? sender, RoutedEventArgs e) =>
        SetStackedMode(ComparisonMode.Blink);

    private void OnDifferenceModeClick(object? sender, RoutedEventArgs e) =>
        SetStackedMode(ComparisonMode.Difference);

    private void OnSplitVerticalClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparisonSplitHorizontal.Value = false;
        }
    }

    private void OnSplitHorizontalClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparisonSplitHorizontal.Value = true;
        }
    }

    private void OnSideBySideStandardClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsSideBySideSynced.Value = false;
        }
    }

    private void OnSideBySideSyncedClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsSideBySideSynced.Value = true;
        }
    }

    private void OnBlinkFastClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparisonBlinkFast.Value = true;
        }
    }

    private void OnBlinkCustomClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparisonBlinkFast.Value = false;
        }
    }

    private async void OnPickAClick(object? sender, RoutedEventArgs e) =>
        await PickImageForSlotAsync(isPrimary: true);

    private async void OnPickBClick(object? sender, RoutedEventArgs e) =>
        await PickImageForSlotAsync(isPrimary: false);

    private async Task PickImageForSlotAsync(bool isPrimary)
    {
        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } localPath)
        {
            return;
        }

        var model = await GetImageModel.GetImageModelAsync(new FileInfo(localPath));
        if (isPrimary)
        {
            tab.SetComparisonPrimary(model);
        }
        else
        {
            tab.SetComparisonSecondary(model);
        }
    }

    private void OnFitClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindAncestorOfType<ImageViewer>() is { } viewer)
        {
            viewer.ResetZoom(Settings.Zoom.IsZoomAnimated);
        }
    }

    private void OnSwapClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabViewModel tab)
        {
            tab.SwapComparisonPair();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparing.Value = false;
            gs.IsSideBySide.Value = false;
        }
    }
}
