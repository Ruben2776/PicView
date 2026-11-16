using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

/// <summary>
/// The compare bar shows in the comparison viewer only, and acts on the tab's comparison.
/// </summary>
[Collection("Sequential")]
public class CompareBarTests
{
    public CompareBarTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void CompareBar_Comparing_IsShownInTheComparisonViewer() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab);

        Assert.True(bar.IsVisible);
        Assert.True(bar.IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void CompareBar_SideBySideOnly_IsNotShown() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        vm.GlobalSettings.IsSideBySide.Value = true;
        window.Content = ViewOf(tab);
        Settle(window);

        Assert.IsType<ImageViewer>(ViewerOf(tab));
        var view = Assert.IsType<NavigableView>(ViewOf(tab));
        Assert.Empty(view.GetVisualDescendants().OfType<CompareBar>());
    });

    [AvaloniaFact]
    public void CompareBar_ZoomPercent_ShowsFactorPercentEvenWithFitScale() => WithWindow((window, _, tab) =>
    {
        var previousAvoidZoomingOut = Settings.Zoom.AvoidZoomingOut;
        var previousIsZoomAnimated = Settings.Zoom.IsZoomAnimated;
        Settings.Zoom.AvoidZoomingOut = true;
        Settings.Zoom.IsZoomAnimated = false;
        try
        {
            tab.Zoom.SetFitScale(0.52);
            var bar = Compare(window, tab, ComparisonMode.Synced);
            var viewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            var percentText = Assert.IsType<TextBlock>(bar.FindControl<TextBlock>("ZoomPercentText"));
            var zoomOutButton = Assert.IsType<Button>(bar.FindControl<Button>("ZoomOutButton"));

            Assert.Equal("100%", percentText.Text);
            Assert.False(zoomOutButton.IsEnabled);

            ((IZoomScrollTarget)viewer).ZoomIn();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("120%", percentText.Text);
            Assert.True(zoomOutButton.IsEnabled);

            ((IZoomScrollTarget)viewer).ZoomOut();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("100%", percentText.Text);
            Assert.False(zoomOutButton.IsEnabled);
        }
        finally
        {
            Settings.Zoom.AvoidZoomingOut = previousAvoidZoomingOut;
            Settings.Zoom.IsZoomAnimated = previousIsZoomAnimated;
        }
    });

    [AvaloniaFact]
    public void CompareBar_BarPinButton_UsesPinAndUnpinTooltips() => WithWindow((window, vm, tab) =>
    {
        var bar = Compare(window, tab);
        var pinButton = Assert.IsType<Button>(bar.FindControl<Button>("BarPinButton"));

        Assert.Equal(vm.Translation.Pin.Value, ToolTip.GetTip(pinButton));

        pinButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(window);

        Assert.Equal(vm.Translation.Unpin.Value, ToolTip.GetTip(pinButton));
    });

    [AvaloniaFact]
    public void CompareBar_SyncedMode_HidesStretchAndAlign() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab, ComparisonMode.Overlay);
        var stretch = Assert.IsType<Button>(bar.FindControl<Button>("StretchButton"));
        var align = Assert.IsType<Button>(bar.FindControl<Button>("AlignButton"));
        Assert.True(stretch.IsVisible);
        Assert.True(align.IsVisible);

        tab.Comparison!.SetMode(ComparisonMode.Synced);
        Settle(window);

        Assert.False(stretch.IsVisible);
        Assert.False(align.IsVisible);
    });

    [AvaloniaFact]
    public void CompareBar_CloseClicked_TurnsOnlyThisTabsComparingOff() => WithWindow((window, vm, tab) =>
    {
        // The harness leaves the Action runner null, so the window's Actions would do nothing
        vm.ActionRunner = new FunctionsMapper(vm, window);
        var bar = Compare(window, tab);
        var close = Assert.IsType<Button>(bar.FindControl<Button>("CloseButton"));

        close.Command!.Execute(close.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        Assert.False(tab.IsComparing.Value);
        Assert.False(vm.GlobalSettings.IsSideBySide.Value);
    });

    // Shows the tab's pair compared, in the mode when given, in its navigable view in the window
    private static CompareBar Compare(TestMainWindow window, TabViewModel tab, ComparisonMode? mode = null)
    {
        window.Width = 800;
        window.Height = 600;
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        tab.SetComparing(true);
        if (mode is { } m)
        {
            tab.Comparison!.SetMode(m);
        }

        window.Content = ViewOf(tab);
        Settle(window);
        var viewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
        return Assert.IsType<CompareBar>(viewer.FindControl<CompareBar>("CompareBar"));
    }
}
