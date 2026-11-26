using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
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
        // Bubbled, so a slider or number box under the pointer takes the wheel first
        BarScroll.AddHandler(PointerWheelChangedEvent, OnBarScrollWheelChanged);
        PickAButton.Classes.Add(HoverClass);
        PickBButton.Classes.Add(HoverClass);
        ResetZoomButton.Classes.Add(HoverClass);
        BarPositionButton.Classes.Add(HoverClass);
        ZoomOutButton.Classes.Add(HoverClass);
        ZoomInButton.Classes.Add(HoverClass);
        ResetAlignButton.Classes.Add(HoverClass);
        CloseButton.Classes.Add(HoverClass);
        _labelledButtons =
        [
            OverlayModeButton, SplitModeButton, SyncedModeButton, BlinkModeButton, DifferenceModeButton,
            BarPositionButton
        ];
        // The row's width or the bar's changed, or a label's text did
        BarScroll.ScrollChanged += (_, _) => UpdateLabels();
        foreach (var button in _labelledButtons)
        {
            button.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextIconButton.TextProperty)
                {
                    UpdateLabels();
                }
            };
        }

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

        if (tab.Comparison is not { } comparison)
        {
            return;
        }

        HoverUnlessActive(OverlayModeButton, comparison.IsOverlay);
        HoverUnlessActive(SplitModeButton, comparison.IsSplit);
        HoverUnlessActive(SyncedModeButton, comparison.IsSynced);
        HoverUnlessActive(BlinkModeButton, comparison.IsBlink);
        HoverUnlessActive(DifferenceModeButton, comparison.IsDifference);
        HoverUnlessActive(SplitVerticalButton, comparison.IsSplitHorizontal, activeWhen: false);
        HoverUnlessActive(SplitHorizontalButton, comparison.IsSplitHorizontal);
        HoverUnlessActive(BlinkFastButton, comparison.IsBlinkFast);
        HoverUnlessActive(BlinkCustomButton, comparison.IsBlinkFast, activeWhen: false);
        HoverUnlessActive(SwapButton, comparison.IsSwapped);
        HoverUnlessActive(PinButton, comparison.IsPinned);
        HoverUnlessActive(StretchButton, comparison.IsStretchFill);
        HoverUnlessActive(AlignButton, comparison.IsAligning);
        HoverUnlessActive(PixelInfoButton, comparison.IsPixelInfo);

        comparison.AlignmentStep.AsObservable()
            .SubscribeOnUIThread(() =>
            {
                var tr = tab.ParentWindowContext.Translation;
                AlignHintText.Text = comparison.AlignmentStep.Value switch
                {
                    1 => tr.AlignStepA1.Value,
                    2 => tr.AlignStepB1.Value,
                    3 => tr.AlignStepA2.Value,
                    4 => tr.AlignStepB2.Value,
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
                Classes.Set("dockTop", position is ComparisonBarPosition.Top);
                Classes.Set("dockLeft", position is ComparisonBarPosition.Left);
                Classes.Set("dockRight", position is ComparisonBarPosition.Right);

                // The position menu opens toward the image, away from the edge the bar is docked to
                if (BarPositionButton.Flyout is PopupFlyoutBase flyout)
                {
                    flyout.Placement = position switch
                    {
                        ComparisonBarPosition.Top => PlacementMode.Bottom,
                        ComparisonBarPosition.Left => PlacementMode.Right,
                        ComparisonBarPosition.Right => PlacementMode.Left,
                        _ => PlacementMode.Top
                    };
                }

                UpdateLabels(force: true);

                var tr = tab.ParentWindowContext.Translation;
                BarPositionButton.Text = position switch
                {
                    ComparisonBarPosition.Top => tr.Top.Value,
                    ComparisonBarPosition.Left => tr.Left.Value,
                    ComparisonBarPosition.Right => tr.Right.Value,
                    _ => tr.Bottom.Value
                };
                // Names the side while the bar is too narrow for the label
                ToolTip.SetTip(BarPositionButton, BarPositionButton.Text);
            }, DebugHelper.LogError(nameof(CompareBar), nameof(ComparisonViewModel.BarPosition)))
            .AddTo(ref _subscriptions);

        // Reports its thickness across its edge for the window fit to reserve. Layout raises this on the UI
        // thread; a hidden bar raises nothing, so its reserve is kept while the gallery is expanded
        Observable.FromEventHandler<SizeChangedEventArgs>(h => SizeChanged += h, h => SizeChanged -= h)
            .Subscribe(_ => ReportExtent(comparison))
            .AddTo(ref _subscriptions);
    }

    // The light theme's hover is the hover class; the dark theme's is the menu item hover
    internal static string HoverClass => Settings.Theme?.Dark is false ? "hover" : "MenuItemHover";

    // A button that can be active takes the hover style only while it is not: while the state is not activeWhen
    private void HoverUnlessActive(Button button, IReadOnlyBindableReactiveProperty<bool> state,
        bool activeWhen = true) =>
        state.AsObservable()
            .SubscribeOnUIThread(() => button.Classes.Set(HoverClass, state.Value != activeWhen),
                DebugHelper.LogError(nameof(CompareBar), nameof(HoverUnlessActive)))
            .AddTo(ref _subscriptions);

    private readonly TextIconButton[] _labelledButtons;

    // The gap between an icon and its label, which goes with the label
    private const double IconGap = 5;

    internal bool LabelsShown { get; private set; } = true;

    // The icon's gap as layout rounds its margin to whole device pixels: 5.33 at 150%, so a share counting 5 would
    // fall short of the labels' real width and flicker between them and the icons
    internal static double IconGapAt(double scale) =>
        LayoutHelper.RoundLayoutThickness(new Thickness(0, 0, IconGap, 0), scale).Right;

    // Decides on the row's width with its labels, whether they are shown or not, so showing or hiding them never
    // changes the answer
    internal static bool LabelsFit(double available, double rowWidth, double labelShare, bool labelsShown) =>
        (labelsShown ? rowWidth : rowWidth + labelShare) <= available + 0.5;

    // At the sides the labels always show; at the top and bottom only while the row with them fits the bar
    private void UpdateLabels(bool force = false)
    {
        var vertical = Classes.Contains("vertical");
        bool show;
        if (vertical)
        {
            show = true;
        }
        else
        {
            // Before layout, or while hidden, there is no width to go on
            if (BarScroll.Viewport.Width <= 0)
            {
                return;
            }

            var gap = UseLayoutRounding ? IconGapAt(LayoutHelper.GetLayoutScale(this)) : IconGap;
            var labelShare = 0d;
            foreach (var button in _labelledButtons)
            {
                if (!string.IsNullOrEmpty(button.Text))
                {
                    labelShare += button.MeasureTextWidth() + gap;
                }
            }

            show = LabelsFit(BarScroll.Viewport.Width, BarRow.DesiredSize.Width, labelShare, LabelsShown);
        }

        if (show == LabelsShown && !force)
        {
            return;
        }

        LabelsShown = show;
        foreach (var button in _labelledButtons)
        {
            button.TextMaxWidth = show ? double.PositiveInfinity : 0;
            button.IconMargin = !show
                ? default
                : vertical && ReferenceEquals(button, BarPositionButton)
                    ? new Thickness(IconGap, 0, IconGap, 0)
                    : new Thickness(0, 0, IconGap, 0);
        }
    }

    private void ReportExtent(ComparisonViewModel comparison)
    {
        // Only while docked in a view's bar host: in its viewer's own host the bar takes no room from the image
        if (Parent is null || this.FindAncestorOfType<ComparisonViewer>() is { } viewer &&
            ReferenceEquals(Parent, viewer.CompareBarHost))
        {
            return;
        }

        comparison.BarExtent.Value = comparison.BarPosition.Value is ComparisonBarPosition.Left
            or ComparisonBarPosition.Right
            ? Bounds.Width
            : Bounds.Height;
    }

    // A scroll viewer moves sideways only for a sideways wheel, so a plain wheel would leave the row at the top or
    // bottom where it is. At the sides the column scrolls with the plain wheel on its own
    private void OnBarScrollWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled || BarScroll.HorizontalScrollBarVisibility is ScrollBarVisibility.Disabled ||
            Math.Abs(e.Delta.X) >= Math.Abs(e.Delta.Y))
        {
            return;
        }

        var maxX = BarScroll.Extent.Width - BarScroll.Viewport.Width;
        if (maxX <= 0)
        {
            return;
        }

        e.Handled = true;
        var offset = BarScroll.Offset;
        BarScroll.SetCurrentValue(ScrollViewer.OffsetProperty,
            offset.WithX(Math.Clamp(offset.X - e.Delta.Y * WheelStep, 0, maxX)));
    }

    // The same distance a scroll viewer moves for one wheel step
    private const double WheelStep = 50;

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

    private void OnPixelInfoClick(object? sender, RoutedEventArgs e) => Comparison?.TogglePixelInfo();

    private void OnBarPositionBottomClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Bottom;

    private void OnBarPositionTopClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Top;

    private void OnBarPositionLeftClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Left;

    private void OnBarPositionRightClick(object? sender, RoutedEventArgs e) =>
        Comparison?.BarPosition.Value = ComparisonBarPosition.Right;
}
