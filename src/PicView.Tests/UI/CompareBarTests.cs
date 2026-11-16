using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using R3;
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
        // The gallery polls its dock position on frames
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
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
        Assert.Empty(view.BarHost.Children);
    });

    [AvaloniaFact]
    public void CompareBar_Comparing_IsInTheBarHost() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        Assert.Same(nav.BarHost, bar.Parent);
        Assert.Single(nav.BarHost.Children);
    });

    [AvaloniaTheory]
    [InlineData(ComparisonBarPosition.Bottom, Dock.Bottom)]
    [InlineData(ComparisonBarPosition.Top, Dock.Top)]
    [InlineData(ComparisonBarPosition.Left, Dock.Left)]
    [InlineData(ComparisonBarPosition.Right, Dock.Right)]
    public void CompareBar_EachPosition_DocksTheHostAndShrinksTheContent(ComparisonBarPosition position, Dock dock)
        => WithWindow((window, _, tab) =>
        {
            Settings.Comparison!.BarPosition = position;
            var bar = Compare(window, tab);
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            // The bar's background may span the edge (its look is the owner's); its row of controls is only as long as
            // its content
            var row = bar.GetVisualDescendants().OfType<StackPanel>().First(p => p.Classes.Contains("barGroup"));

            Assert.Equal(dock, DockPanel.GetDock(nav.BarHost));
            if (position is ComparisonBarPosition.Top or ComparisonBarPosition.Bottom)
            {
                Assert.True(nav.BarHost.Bounds.Height > 0);
                Assert.Equal(nav.Bounds.Height - nav.BarHost.Bounds.Height, nav.ContentPanel.Bounds.Height, precision: 1);
                Assert.True(row.DesiredSize.Width < nav.Bounds.Width);
            }
            else
            {
                Assert.True(nav.BarHost.Bounds.Width > 0);
                Assert.Equal(nav.Bounds.Width - nav.BarHost.Bounds.Width, nav.ContentPanel.Bounds.Width, precision: 1);
                Assert.True(row.DesiredSize.Height < nav.Bounds.Height);
            }
        });

    [AvaloniaFact]
    public void CompareBar_PositionChanged_MovesTheHost() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        tab.Comparison!.BarPosition.Value = ComparisonBarPosition.Top;
        Settle(window);

        Assert.Equal(Dock.Top, DockPanel.GetDock(nav.BarHost));
    });

    [AvaloniaFact]
    public void CompareBar_SameSideAsGallery_TouchesTheGallerysInnerEdge() => WithWindow((window, vm, tab) =>
    {
        Settings.Gallery!.DockPosition = GalleryDockPosition.Bottom;
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        tab.Gallery.Initialize(vm.GallerySettings);
        Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        tab.Gallery.Switcher.Start(docked: true);
        Settle(window);

        Assert.True(nav.GalleryView.Bounds.Height > 0);
        Assert.Equal(BoundsIn(nav, nav.GalleryView).Top, BoundsIn(nav, nav.BarHost).Bottom, precision: 1);
    });

    [AvaloniaFact]
    public void HoverBar_BarAtBottom_IsAboveTheBar() => WithWindow((window, vm, tab) =>
    {
        vm.GlobalSettings.ShowBottomBar.Value = false;
        vm.GlobalSettings.ShowHoverBar.Value = true;
        vm.GlobalSettings.ShowFadeButtons.Value = true;
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        Assert.True(BoundsIn(nav, nav.HoverBar).Bottom <= BoundsIn(nav, nav.BarHost).Top + 0.5);
    });

    [AvaloniaFact]
    public void CompareBar_GalleryExpanded_IsHiddenAndKeepsItsExtent() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        var extentBefore = tab.Comparison!.BarExtent.Value;
        Assert.True(extentBefore > 0);

        tab.Gallery.IsGalleryExpanded.Value = true;
        Settle(window);
        Assert.False(nav.BarHost.IsVisible);
        Assert.Equal(extentBefore, tab.Comparison!.BarExtent.Value);

        tab.Gallery.IsGalleryExpanded.Value = false;
        Settle(window);
        Assert.True(bar.IsEffectivelyVisible);
        Assert.Equal(extentBefore, tab.Comparison!.BarExtent.Value);
    });

    [AvaloniaTheory]
    [InlineData(ComparisonBarPosition.Bottom)]
    [InlineData(ComparisonBarPosition.Left)]
    public void CompareBar_Docked_ReportsItsThickness(ComparisonBarPosition position) => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = position;
        Compare(window, tab);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));

        var thickness = position is ComparisonBarPosition.Left ? nav.BarHost.Bounds.Width : nav.BarHost.Bounds.Height;
        Assert.True(thickness > 0);
        Assert.InRange(tab.Comparison!.BarExtent.Value, thickness - 1, thickness + 1);
    });

    [AvaloniaFact]
    public void CompareBar_InterfaceHidden_StaysShown() => WithWindow((window, vm, tab) =>
    {
        var bar = Compare(window, tab);

        vm.GlobalSettings.ShowInterface.Value = false;
        Settle(window);

        Assert.True(bar.IsEffectivelyVisible);
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
    public void CompareBar_Comparing_HasNoBarPinButton() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab);
        Assert.Null(bar.FindControl<Button>("BarPinButton"));
        Assert.NotNull(bar.FindControl<Button>("PinButton")); // pins the pair; it stays
    });

    [AvaloniaFact]
    public void CompareBar_AfterInactivity_StaysShown() => WithWindow((window, _, tab) =>
    {
        var bar = Compare(window, tab);
        Thread.Sleep(3500); // longer than the old 3 s auto-hide
        Settle(window);
        Assert.Equal(1, bar.Opacity);
        Assert.True(bar.IsHitTestVisible);
        Assert.True(bar.IsEffectivelyVisible);
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

    [AvaloniaTheory]
    [InlineData(ComparisonBarPosition.Left)]
    [InlineData(ComparisonBarPosition.Right)]
    public void CompareBar_AtTheSide_StacksItsGroupsVertically(ComparisonBarPosition position)
        => WithWindow((window, _, tab) =>
        {
            Settings.Comparison!.BarPosition = position;
            var bar = Compare(window, tab, ComparisonMode.Overlay);

            AssertGroupsAndSlider(bar, Orientation.Vertical);
            var track = bar.GetVisualDescendants().OfType<Slider>().Single()
                .GetVisualDescendants().OfType<Track>().Single();
            Assert.True(track.Bounds.Height > 0);
            var strip = Assert.IsType<Border>(bar.Content);
            Assert.True(strip.Bounds.Height > strip.Bounds.Width);
        });

    [AvaloniaFact]
    public void CompareBar_AtTheBottom_StacksItsGroupsHorizontally() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        var bar = Compare(window, tab, ComparisonMode.Overlay);

        AssertGroupsAndSlider(bar, Orientation.Horizontal);
        var track = bar.GetVisualDescendants().OfType<Slider>().Single()
            .GetVisualDescendants().OfType<Track>().Single();
        Assert.True(track.Bounds.Width > 0);
        var strip = Assert.IsType<Border>(bar.Content);
        Assert.True(strip.Bounds.Width > strip.Bounds.Height);
    });

    [AvaloniaFact]
    public void CompareBar_PositionItemClicked_SetsTheBarPositionAndSaves() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        var bar = Compare(window, tab);
        var button = Assert.IsType<Button>(bar.FindControl<Button>("BarPositionButton"));
        var menu = Assert.IsType<MenuFlyout>(button.Flyout);

        menu.Items.OfType<MenuItem>().Single(i => string.Equals(i.Name, "BarPositionLeftItem", StringComparison.Ordinal))
            .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Settle(window);

        Assert.Equal(ComparisonBarPosition.Left, tab.Comparison!.BarPosition.Value);
        Assert.Equal(ComparisonBarPosition.Left, Settings.Comparison!.BarPosition);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));
        Assert.Equal(Dock.Left, DockPanel.GetDock(nav.BarHost));
    });

    [AvaloniaTheory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void CompareBar_AtTheSide_OpacityRunsFromAAtTheTopToBAtTheBottom(double opacity)
        => WithWindow((window, _, tab) =>
        {
            Settings.Comparison!.BarPosition = ComparisonBarPosition.Left;
            var bar = Compare(window, tab, ComparisonMode.Overlay);
            tab.Comparison!.OverlayOpacity.Value = opacity;
            Settle(window);

            var track = bar.GetVisualDescendants().OfType<Slider>().Single()
                .GetVisualDescendants().OfType<Track>().Single();
            var thumb = Assert.IsType<Thumb>(track.Thumb);
            var top = thumb.TranslatePoint(default, track)!.Value.Y;
            if (opacity is 0)
            {
                Assert.InRange(top, -0.5, 0.5);
            }
            else
            {
                Assert.InRange(top + thumb.Bounds.Height, track.Bounds.Height - 0.5, track.Bounds.Height + 0.5);
            }
        });

    [AvaloniaFact]
    public void CompareBar_AtTheSideAligning_KeepsItsThickness() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Left;
        var bar = Compare(window, tab, ComparisonMode.Overlay);
        TallEnoughNotToScroll(window);
        var comparison = tab.Comparison!;
        var before = comparison.BarExtent.Value;
        Assert.True(before > 0);

        comparison.ToggleAligning();
        Settle(window);
        AssertDoesNotScroll(bar);
        Assert.InRange(comparison.BarExtent.Value, before - 0.5, before + 0.5);

        comparison.AddAlignmentPoint(10, 10);
        Settle(window);
        Assert.Equal(2, comparison.AlignmentStep.Value);
        AssertDoesNotScroll(bar);
        Assert.InRange(comparison.BarExtent.Value, before - 0.5, before + 0.5);
    });

    [AvaloniaFact]
    public void CompareBar_AtTheSideCustomBlink_KeepsItsThickness() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Left;
        var bar = Compare(window, tab, ComparisonMode.Blink);
        TallEnoughNotToScroll(window);
        var comparison = tab.Comparison!;
        comparison.IsBlinkFast.Value = true;
        Settle(window);
        var before = comparison.BarExtent.Value;
        Assert.True(before > 0);

        comparison.IsBlinkFast.Value = false;
        Settle(window);

        AssertDoesNotScroll(bar);
        Assert.InRange(comparison.BarExtent.Value, before - 0.5, before + 0.5);
    });

    [AvaloniaFact]
    public void CompareBar_AtTheBottomAligning_ShowsTheHintOnOneLine() => WithWindow((window, _, tab) =>
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Bottom;
        var bar = Compare(window, tab, ComparisonMode.Overlay);
        var hint = Assert.IsType<TextBlock>(bar.FindControl<TextBlock>("AlignHintText"));

        tab.Comparison!.ToggleAligning();
        Settle(window);

        Assert.False(string.IsNullOrEmpty(hint.Text));
        Assert.Single(hint.TextLayout.TextLines);
    });

    // Every group panel and the opacity slider run in the orientation
    private static void AssertGroupsAndSlider(CompareBar bar, Orientation orientation)
    {
        var groups = bar.GetVisualDescendants().OfType<StackPanel>()
            .Where(p => p.Classes.Contains("barGroup")).ToList();
        Assert.NotEmpty(groups);
        Assert.All(groups, p => Assert.Equal(orientation, p.Orientation));
        Assert.Equal(orientation, bar.GetVisualDescendants().OfType<Slider>().Single().Orientation);
    }

    // A column taller than the window scrolls, and its scroll bar adds to the side bar's thickness; these tests
    // look only at what the rows themselves do
    private static void TallEnoughNotToScroll(TestMainWindow window)
    {
        window.Height = 1200;
        Settle(window);
    }

    private static void AssertDoesNotScroll(CompareBar bar)
    {
        var scroll = bar.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroll.Extent.Height <= scroll.Viewport.Height + 0.5);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    private static Rect BoundsIn(Visual root, Control control) =>
        new(control.TranslatePoint(default, root)!.Value, control.Bounds.Size);

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
