using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Core.Config;
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
        if (DataContext is TabViewModel tab)
        {
            var tr = tab.ParentWindowContext.Translation;
            ToolTip.SetTip(BarPinButton, _isBarPinned ? tr.Unpin.Value : tr.Pin.Value);
        }
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
            return;
        }

        tab.Zoom.FactorPercent
            .Subscribe(percent =>
            {
                ZoomPercentText.Text = $"{percent}%";
                ZoomOutButton.IsEnabled = !(Settings.Zoom.AvoidZoomingOut && tab.Zoom.Factor <= 1.001);
            })
            .AddTo(ref _subscriptions);

        if (tab.Comparison is { } comparison)
        {
            comparison.AlignmentStep.AsObservable()
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

        NotifyUserActivity();
    }

    private ComparisonViewModel? Comparison => (DataContext as TabViewModel)?.Comparison;

    private void OnSplitVerticalClick(object? sender, RoutedEventArgs e) => Comparison?.IsSplitHorizontal.Value = false;

    private void OnSplitHorizontalClick(object? sender, RoutedEventArgs e) => Comparison?.IsSplitHorizontal.Value = true;

    private void OnBlinkFastClick(object? sender, RoutedEventArgs e) => Comparison?.IsBlinkFast.Value = true;

    private void OnBlinkCustomClick(object? sender, RoutedEventArgs e) => Comparison?.IsBlinkFast.Value = false;

    private void OnStretchToggleClick(object? sender, RoutedEventArgs e)
    {
        if (Comparison is { } comparison)
        {
            comparison.IsStretchFill.Value = !comparison.IsStretchFill.Value;
        }
    }

    private void OnAlignClick(object? sender, RoutedEventArgs e) => Comparison?.ToggleAligning();

    private void OnResetAlignClick(object? sender, RoutedEventArgs e) => Comparison?.ResetAlignment();
}
