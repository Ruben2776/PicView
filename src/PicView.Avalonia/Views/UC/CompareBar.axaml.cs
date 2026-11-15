using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class CompareBar : UserControl, IDisposable
{
    private DisposableBag _subscriptions;
    private DispatcherTimer? _autoHideTimer;
    private bool _isBarPinned;

    public CompareBar()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToTab();
        AttachedToVisualTree += (_, _) => SubscribeToTab();
        DetachedFromVisualTree += (_, _) => Dispose();
        PointerMoved += (_, _) => NotifyUserActivity();
    }

    public void Dispose()
    {
        StopAutoHideTimer();
        _subscriptions.Dispose();
        _subscriptions = default;
    }

    public void NotifyUserActivity()
    {
        if (!IsVisible)
        {
            return;
        }

        Opacity = 1;
        IsHitTestVisible = true;
        if (_isBarPinned)
        {
            StopAutoHideTimer();
            return;
        }

        RestartAutoHideTimer();
    }

    private void RestartAutoHideTimer()
    {
        if (_autoHideTimer is null)
        {
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _autoHideTimer.Tick += OnAutoHideTick;
        }

        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    private void StopAutoHideTimer()
    {
        if (_autoHideTimer is not null)
        {
            _autoHideTimer.Stop();
            _autoHideTimer.Tick -= OnAutoHideTick;
            _autoHideTimer = null;
        }
    }

    private void OnAutoHideTick(object? sender, EventArgs e)
    {
        _autoHideTimer?.Stop();
        if (!_isBarPinned && IsVisible && !IsPointerOver)
        {
            Opacity = 0;
            IsHitTestVisible = false;
        }
    }

    private void OnBarPinClick(object? sender, RoutedEventArgs e)
    {
        _isBarPinned = !_isBarPinned;
        BarPinButton.Classes.Set("active", _isBarPinned);
        if (_isBarPinned)
        {
            StopAutoHideTimer();
            Opacity = 1;
            IsHitTestVisible = true;
        }
        else
        {
            RestartAutoHideTimer();
        }
    }

    private void SubscribeToTab()
    {
        _subscriptions.Dispose();
        _subscriptions = default;

        if (DataContext is not TabViewModel tab)
        {
            IsVisible = false;
            return;
        }

        tab.ParentWindowContext.GlobalSettings.IsShowingPair
            .Subscribe(visible =>
            {
                IsVisible = visible;
                if (visible)
                {
                    NotifyUserActivity();
                }
                else
                {
                    StopAutoHideTimer();
                }
            })
            .AddTo(ref _subscriptions);

        tab.Zoom.Level
            .Subscribe(level =>
            {
                ZoomPercentText.Text = $"{level:0}%";
                ZoomOutButton.IsEnabled = !(Settings.Zoom.AvoidZoomingOut && level <= 100.5);
            })
            .AddTo(ref _subscriptions);

        tab.AlignmentStep
            .Subscribe(step =>
            {
                var tr = tab.ParentWindowContext.Translation;
                AlignHintText.Text = step switch
                {
                    1 => tr.ComparisonAlignStepA1.Value,
                    2 => tr.ComparisonAlignStepB1.Value,
                    3 => tr.ComparisonAlignStepA2.Value,
                    4 => tr.ComparisonAlignStepB2.Value,
                    _ => string.Empty
                };
            })
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

    private void OnPickAClick(object? sender, RoutedEventArgs e) =>
        _ = PickImageForSlotSafeAsync(isPrimary: true);

    private void OnPickBClick(object? sender, RoutedEventArgs e) =>
        _ = PickImageForSlotSafeAsync(isPrimary: false);

    private async Task PickImageForSlotSafeAsync(bool isPrimary)
    {
        try
        {
            await PickImageForSlotAsync(isPrimary).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(CompareBar), nameof(PickImageForSlotSafeAsync), ex);
        }
    }

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
            FileTypeFilter = [GetFilePickerFileTypes.AllFileType]
        }).ConfigureAwait(true);

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } localPath)
        {
            return;
        }

        var model = await GetImageModel.GetImageModelAsync(new FileInfo(localPath)).ConfigureAwait(true);
        if (isPrimary)
        {
            tab.SetComparisonPrimary(model);
        }
        else
        {
            tab.SetComparisonSecondary(model);
        }
    }

    private void OnPinClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabViewModel tab)
        {
            tab.ToggleComparisonPin();
        }
    }

    private void OnStretchToggleClick(object? sender, RoutedEventArgs e)
    {
        if (SettingsVm is { } gs)
        {
            gs.IsComparisonStretchFill.Value = !gs.IsComparisonStretchFill.Value;
        }
    }

    private void OnAlignClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.IsAligning.Value)
        {
            tab.IsAligning.Value = false;
            tab.AlignmentStep.Value = 0;
        }
        else
        {
            tab.IsAligning.Value = true;
            tab.AlignmentStep.Value = 1;
        }
    }

    private void OnResetAlignClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabViewModel tab)
        {
            tab.ResetComparisonAlignment();
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
        if (DataContext is TabViewModel tab)
        {
            tab.ClearComparisonOverrides();
        }
        if (SettingsVm is { } gs)
        {
            gs.IsComparing.Value = false;
            gs.IsSideBySide.Value = false;
        }
    }
}
