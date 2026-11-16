using Avalonia.Controls;
using Avalonia.Interactivity;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class CompareBar : UserControl, IDisposable
{
    private DisposableBag _subscriptions;

    public CompareBar()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToTab();
        AttachedToVisualTree += (_, _) => SubscribeToTab();
        DetachedFromVisualTree += (_, _) => Dispose();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _subscriptions = default;
    }

    private void SubscribeToTab()
    {
        _subscriptions.Dispose();
        _subscriptions = default;

        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        // The tab's zoom and comparison may change on the thread that shows the tab's next image
        tab.Zoom.FactorPercent
            .SubscribeOnUIThread(() =>
            {
                ZoomPercentText.Text = $"{tab.Zoom.FactorPercent.CurrentValue}%";
                ZoomOutButton.IsEnabled = !(Settings.Zoom.AvoidZoomingOut && tab.Zoom.Factor <= 1.001);
            }, DebugHelper.LogError(nameof(CompareBar), nameof(TabZoom.FactorPercent)))
            .AddTo(ref _subscriptions);

        if (tab.Comparison is { } comparison)
        {
            comparison.AlignmentStep.AsObservable()
                .SubscribeOnUIThread(() =>
                {
                    var tr = tab.ParentWindowContext.Translation;
                    AlignHintText.Text = comparison.AlignmentStep.Value switch
                    {
                        1 => tr.ComparisonAlignStepA1.Value,
                        2 => tr.ComparisonAlignStepB1.Value,
                        3 => tr.ComparisonAlignStepA2.Value,
                        4 => tr.ComparisonAlignStepB2.Value,
                        _ => string.Empty
                    };
                }, DebugHelper.LogError(nameof(CompareBar), nameof(ComparisonViewModel.AlignmentStep)))
                .AddTo(ref _subscriptions);

            // Stacks the groups into a column at the sides, and names the current side on the position button
            comparison.BarPosition
                .SubscribeOnUIThread(() =>
                {
                    var position = comparison.BarPosition.Value;
                    Classes.Set("vertical", position is ComparisonBarPosition.Left or ComparisonBarPosition.Right);
                    var tr = tab.ParentWindowContext.Translation;
                    BarPositionButton.Content = position switch
                    {
                        ComparisonBarPosition.Top => tr.Top.Value,
                        ComparisonBarPosition.Left => tr.Left.Value,
                        ComparisonBarPosition.Right => tr.Right.Value,
                        _ => tr.Bottom.Value
                    };
                }, DebugHelper.LogError(nameof(CompareBar), nameof(ComparisonViewModel.BarPosition)))
                .AddTo(ref _subscriptions);
        }
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

    private void OnBarPositionBottomClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Bottom;

    private void OnBarPositionTopClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Top;

    private void OnBarPositionLeftClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Left;

    private void OnBarPositionRightClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Right;
}
