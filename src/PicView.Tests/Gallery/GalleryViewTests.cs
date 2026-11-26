using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;
using R3;
using Orientation = Avalonia.Layout.Orientation;

namespace PicView.Tests.Gallery;

/// <summary>
/// How the gallery view shows the items of its tab's gallery as they change.
/// </summary>
[Collection("Sequential")]
public class GalleryViewTests
{
    public GalleryViewTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void Sorted_ShownGallery_ShowsItsItemsInTheNewOrder()
    {
        WithGalleryView((tab, viewer) =>
        {
            var fileA = new FileInfo("a.jpg");
            var fileB = new FileInfo("b.jpg");
            var fileC = new FileInfo("c.jpg");
            var a = new GalleryItemViewModel { FileInfo = fileA };
            var b = new GalleryItemViewModel { FileInfo = fileB };
            var c = new GalleryItemViewModel { FileInfo = fileC };
            tab.Gallery.GalleryItems.AddRange([a, b, c]);
            Dispatcher.UIThread.RunJobs();

            var order = new[] { fileC, fileA, fileB };
            tab.Gallery.GalleryItems.Sort(Comparer<GalleryItemViewModel>.Create((x, y) =>
                Array.IndexOf(order, x.FileInfo).CompareTo(Array.IndexOf(order, y.FileInfo))));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([c, a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Cleared_ShownGallery_ShowsNoItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            Dispatcher.UIThread.RunJobs();

            tab.Gallery.GalleryItems.Clear();
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewer.Items);
        });
    }

    [AvaloniaFact]
    public void Replaced_ShownGallery_ShowsTheNewItem()
    {
        WithGalleryView((tab, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.AddRange([a, b]);
            Dispatcher.UIThread.RunJobs();
            var rebuilt = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };

            tab.Gallery.GalleryItems[1] = rebuilt;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, rebuilt], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Initialize_ForATabThatIsNotTheActiveOne_ShowsThatTabsItems()
    {
        WithGalleryView(vm =>
        {
            var first = vm.WindowTabs.ActiveTab.CurrentValue;
            var second = vm.WindowTabs.CreateTab();
            vm.WindowTabs.SelectTab(first);
            return second;
        }, (tab, vm, viewer) =>
        {
            var active = vm.WindowTabs.ActiveTab.CurrentValue;
            Assert.NotSame(tab, active);
            var own = new GalleryItemViewModel { FileInfo = new FileInfo("own.jpg") };

            tab.Gallery.GalleryItems.Add(own);
            active.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("other.jpg") });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([own], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Released_NavigableView_StopsFollowingItsTabsGallery()
    {
        WithNavigableView((tab, _, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.AddRange([a, b]);
            Dispatcher.UIThread.RunJobs();
            tab.ParentWindowContext.WindowTabs.CreateTab();

            tab.ParentWindowContext.WindowTabs.CloseTab(tab);
            tab.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("c.jpg") });
            tab.Gallery.NavigateGalleryCommand.Execute(NavigateTo.Last);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, b], viewer.Items.Cast<GalleryItemViewModel>());
            Assert.Equal(-1, viewer.SelectedItemIndex);
        });
    }

    [AvaloniaFact]
    public void CroppedAndBack_NavigableView_StillFollowsItsTabsGallery()
    {
        WithNavigableView((tab, _, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            tab.Crop = null;
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.Add(b);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void StartedDocked_ShowsDockedLook()
    {
        WithShownDockedGallery((_, _, view, panel) =>
        {
            var size = Settings.Gallery.DockedGalleryItemSize;
            Assert.Equal(size, panel.ItemHeight);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(size), view.Height);
            Assert.Equal(Orientation.Horizontal, panel.Orientation);

            Assert.NotEmpty(panel.RealizedItems);
            var container = Assert.IsType<ContentPresenter>(panel.RealizedItems[0].Element);
            var item = Assert.IsType<GalleryItem>(container.Child);
            Assert.Equal(panel.ItemHeight, item.Height);
            Assert.Equal(GalleryThumbnailStretch.ToStretch(Settings.Gallery.DockedGalleryStretchMode),
                item.FindControl<ThumbImage>("GalleryImage")!.Stretch);
        });
    }

    [AvaloniaFact]
    public void DockedItemSizeChanged_WhileDocked_ResizesByTheDefaultsFormula()
    {
        WithShownDockedGallery((core, window, view, panel) =>
        {
            WindowGalleryOf(view).DockedGalleryItemSize.Value = 120;
            RenderFrame(window);

            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(120), view.Height);
            Assert.Equal(120, panel.ItemHeight);
        });
    }

    [AvaloniaFact]
    public void HiddenByCrop_KeepsItemHeight()
    {
        WithShownDockedGallery((_, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            var itemHeight = panel.ItemHeight;

            // Crop hides the gallery without animation
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            RenderFrame(window);

            Assert.False(view.IsVisible);
            Assert.Equal(itemHeight, panel.ItemHeight);
            Assert.True(panel.ItemHeight > 0);
        });
    }

    [AvaloniaFact]
    public void StartedDocked_ViewLoadsBeforeInitialize_ShowsDockedGallery()
    {
        // As at startup, where the tab starts docked and its viewer loads before the files are read
        WithShownDockedGallery((core, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            tab.Gallery.Initialize(core.GallerySettings);
            RenderFrame(window);

            Assert.True(view.IsVisible);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(Settings.Gallery.DockedGalleryItemSize), view.Height);
            Assert.Equal(Settings.Gallery.DockedGalleryItemSize, panel.ItemHeight);
        }, initializeFirst: false);
    }

    [AvaloniaFact]
    public void HiddenWhileLoading_StartedDocked_EndsHidden()
    {
        // A Single Image hides the gallery while the view loads
        WithShownDockedGallery((_, _, view, _) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            Assert.Equal(GalleryMode.Closed, tab.Gallery.ShownMode.Value);
            Assert.False(view.IsVisible);
        }, duringLoad: tab => tab.Gallery.Switcher.SetHidden(true));
    }

    [AvaloniaFact]
    public void ShownWhileLoading_StartedHidden_EndsDocked()
    {
        WithShownDockedGallery((_, _, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            Assert.Equal(GalleryMode.Docked, tab.Gallery.ShownMode.Value);
            Assert.True(view.IsVisible);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(Settings.Gallery.DockedGalleryItemSize), view.Height);
            Assert.Equal(Settings.Gallery.DockedGalleryItemSize, panel.ItemHeight);
        }, startHidden: true, duringLoad: tab => tab.Gallery.Switcher.SetHidden(false));
    }

    [AvaloniaFact]
    public void DisplayModeButtons_Toggled_SwapWhichOneShows()
    {
        WithShownDockedGallery((core, _, view, _) =>
        {
            var gallery = WindowGalleryOf(view);
            gallery.Start();
            var toggle = gallery.ToggleExpandedDisplayModeCommand;
            var toTiles = view.FindControl<IconButton>("ToTilesButton")!;
            var toGrid = view.FindControl<IconButton>("ToGridButton")!;
            Assert.Same(toggle, toTiles.Command);
            Assert.Same(toggle, toGrid.Command);
            Assert.True(toTiles.IsVisible);
            Assert.False(toGrid.IsVisible);

            toggle.Execute(Unit.Default);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(GalleryDisplayMode.Masonry, gallery.ExpandedGalleryDisplayMode.Value);
            Assert.False(toTiles.IsVisible);
            Assert.True(toGrid.IsVisible);

            toggle.Execute(Unit.Default);
            Dispatcher.UIThread.RunJobs();

            Assert.True(toTiles.IsVisible);
            Assert.False(toGrid.IsVisible);
        });
    }

    // The toggle hides the button it was run from, and nothing stops a hidden button's repeat timer, so a repeating
    // button would switch back and forth for as long as it is held. The timer is a DispatcherTimer, which the headless
    // dispatcher does not run from RunJobs, so the held press itself is not driven here
    [AvaloniaFact]
    public void DisplayModeButtons_DoNotRepeatWhileHeld()
    {
        WithShownDockedGallery((_, _, view, _) =>
        {
            Assert.False(view.FindControl<IconButton>("ToTilesButton")!.IsRepeatEnabled);
            Assert.False(view.FindControl<IconButton>("ToGridButton")!.IsRepeatEnabled);
        });
    }

    [AvaloniaFact]
    public void ExpandedBeforeTheViewLoads_FillsItsParent()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Grid, (_, _, view, _) =>
        {
            Assert.True(view.IsVisible);
            Assert.Equal(((Control)view.Parent!).Bounds.Size, view.Bounds.Size);
        });
    }

    [AvaloniaFact]
    public void Expanded_Masonry_HasTheTilesClassAndScrollsVertically()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Masonry, (_, _, view, panel) =>
        {
            var scrollViewer = panel.FindAncestorOfType<ScrollViewer>()!;
            Assert.Contains("tiles", view.Classes);
            Assert.DoesNotContain("grid", view.Classes);
            Assert.Equal(GalleryDisplayMode.Masonry, panel.DisplayMode);
            Assert.Equal(VerticalAlignment.Top, panel.VerticalAlignment);
            Assert.Equal(ScrollBarVisibility.Visible, scrollViewer.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);
            // Below the 85x60 button cluster
            Assert.Equal(60, VerticalScrollBar(panel).Bounds.Top);
        });
    }

    [AvaloniaFact]
    public void Expanded_Grid_HasTheGridClassNotTheTilesClassAndScrollsVertically()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Grid, (_, _, view, panel) =>
        {
            var scrollViewer = panel.FindAncestorOfType<ScrollViewer>()!;
            Assert.Contains("grid", view.Classes);
            Assert.DoesNotContain("tiles", view.Classes);
            Assert.Equal(VerticalAlignment.Top, panel.VerticalAlignment);
            Assert.Equal(ScrollBarVisibility.Visible, scrollViewer.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);
            // Below the 85x60 button cluster
            Assert.Equal(60, VerticalScrollBar(panel).Bounds.Top);
        });
    }

    [AvaloniaFact]
    public void Docked_WhateverTheDisplayMode_NeverHasTheTilesClass()
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = GalleryDisplayMode.Masonry;
        WithShownDockedGallery((core, window, view, panel) =>
        {
            Assert.DoesNotContain("tiles", view.Classes);
            WindowGalleryOf(view).ExpandedGalleryDisplayMode.Value = GalleryDisplayMode.Rows;
            RenderFrame(window);
            Assert.DoesNotContain("tiles", view.Classes);
            // Its record is in Grid, and it is still no grid
            Assert.DoesNotContain("grid", view.Classes);
            Assert.DoesNotContain("expanded", view.Classes);
            Assert.Equal(GalleryDisplayMode.Grid, panel.DisplayMode);
        });
    }

    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void GetExpandedWrapWidth_OnceExpanded_EqualsThePanelsWidth(double renderScaling)
    {
        WithShownExpandedGallery(GalleryDisplayMode.Masonry, (_, _, view, panel) =>
        {
            var appearance = ((TabViewModel)view.DataContext!).Gallery.Appearance.Value;
            Assert.True(panel.Bounds.Width > 0);
            Assert.Equal(panel.Bounds.Width, view.GetExpandedWrapWidth(appearance), 1e-6);
        }, renderScaling);
    }

    [AvaloniaFact]
    public void DisplayModeChanged_WhileExpanded_ReappliesWithoutAnAnimation()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Grid, (core, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            WindowGalleryOf(view).ExpandedGalleryDisplayMode.Value = GalleryDisplayMode.Masonry;
            RenderFrame(window);

            Assert.False(view.IsInAnimation);
            Assert.Contains("tiles", view.Classes);
            Assert.Equal(GalleryDisplayMode.Masonry, panel.DisplayMode);
            Assert.True(double.IsNaN(panel.WrapWidthOverride));
            Assert.Equal(view.GetExpandedWrapWidth(tab.Gallery.Appearance.Value), panel.Bounds.Width, 1e-6);

            WindowGalleryOf(view).ExpandedGalleryDisplayMode.Value = GalleryDisplayMode.Grid;
            RenderFrame(window);

            Assert.False(view.IsInAnimation);
            Assert.DoesNotContain("tiles", view.Classes);
            Assert.Equal(60, VerticalScrollBar(panel).Margin.Top); // below the button cluster in every display mode
        });
    }

    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void ExpandedFromClosed_Masonry_LocksTheWrapSoNoItemMovesWhenTheAnimationEnds(double renderScaling)
    {
        WithShownExpandedGallery(GalleryDisplayMode.Masonry, (_, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            tab.Gallery.Switcher.ToggleExpanded();
            Dispatcher.UIThread.RunJobs();

            // Growing from nothing, the panel already wraps in the width it ends with: the second item is beside the first
            Assert.True(view.IsInAnimation);
            var locked = view.GetExpandedWrapWidth(tab.Gallery.Appearance.Value);
            Assert.Equal(locked, panel.WrapWidthOverride, 1e-6);
            var during = panel.ItemBounds.ToArray();
            Assert.Equal(0, during[1].Top);

            WaitForAnimation(window, view);

            Assert.True(double.IsNaN(panel.WrapWidthOverride));
            Assert.Equal(locked, panel.Bounds.Width, 1e-6);
            Assert.Equal(during, panel.ItemBounds.ToArray());
        }, renderScaling, startClosed: true);
    }

    // Review Focus 2. From a side dock only the width tweens, starting at the docked width, where Masonry has room for
    // one column and Rows for one item a row. The frames in between are read too
    [AvaloniaTheory]
    [InlineData(GalleryDockPosition.Left, GalleryDisplayMode.Masonry, 1.0)]
    [InlineData(GalleryDockPosition.Right, GalleryDisplayMode.Masonry, 1.25)]
    [InlineData(GalleryDockPosition.Left, GalleryDisplayMode.Masonry, 1.5)]
    [InlineData(GalleryDockPosition.Right, GalleryDisplayMode.Rows, 1.0)]
    [InlineData(GalleryDockPosition.Left, GalleryDisplayMode.Rows, 1.25)]
    public void ExpandedFromASideDock_Tiles_KeepTheFinalWrapThroughTheWidthTweenAndMoveNothingWhenItEnds(
        GalleryDockPosition dock, GalleryDisplayMode displayMode, double renderScaling)
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = displayMode;
        WithShownDockedGallery((_, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            var dockedWidth = view.Width;
            Assert.Equal(dock, tab.Gallery.Appearance.Value.Dock);
            Assert.Equal(GalleryDefaults.GetDockedGalleryWidth(Settings.Gallery.DockedGalleryItemSize), dockedWidth);
            Assert.Equal(GalleryDisplayMode.Grid, panel.DisplayMode);

            tab.Gallery.Switcher.ToggleExpanded();
            Dispatcher.UIThread.RunJobs();

            // Still as narrow as the strip, and already wrapped in the width it ends with: the second item is beside the
            // first, past the strip's edge
            Assert.True(view.IsInAnimation);
            var locked = view.GetExpandedWrapWidth(tab.Gallery.Appearance.Value);
            Assert.Equal(locked, panel.WrapWidthOverride, 1e-6);
            var during = panel.ItemBounds.ToArray();
            Assert.Equal(0, during[1].Top);
            Assert.True(during[1].Left > dockedWidth);

            // Every frame until the control is done: no item is placed anew. The tween runs on the render timer's real time
            var endWidth = ((Control)view.Parent!).Bounds.Width;
            var framesMidTween = 0;
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (view.IsInAnimation && DateTime.UtcNow < deadline)
            {
                RenderFrame(window);
                if (view.IsInAnimation)
                {
                    Assert.Equal(during, panel.ItemBounds.ToArray());
                    if (view.Bounds.Width > dockedWidth + 1 && view.Bounds.Width < endWidth - 1)
                    {
                        framesMidTween++;
                    }
                }

                Thread.Sleep(10);
            }

            RenderFrame(window);
            Assert.False(view.IsInAnimation);

            Assert.True(double.IsNaN(panel.WrapWidthOverride));
            Assert.Equal(locked, panel.Bounds.Width, 1e-6);
            Assert.Equal(during, panel.ItemBounds.ToArray());

            // The tween runs on real time, so a stalled machine can jump from the docked width straight to the full
            // width. Then the frames in between were not checked, which is no failure of the gallery
            if (framesMidTween is 0)
            {
                Assert.Skip("No frame was read between the docked width and the full width, so the tween was not checked");
            }
        }, beforeLoad: (tab, window) =>
        {
            window.Width = 1000;
            window.Height = 600;
            window.SetRenderScaling(renderScaling);
            // The helper docks at the bottom. The tab starts again, docked at its window's other edge
            tab.ParentWindowContext.WindowSettings.Gallery.DockPosition.Value = dock;
            tab.Gallery.Switcher.Start(docked: false);
            tab.Gallery.Switcher.Start(docked: true);
        });
    }

    // Review Focus 5, at the view: nothing is applied while crop or a Single Image hides the gallery
    [AvaloniaFact]
    public void DisplayModeChanged_WhileTheExpandedGalleryIsHidden_IsAppliedWhenItShowsAgain()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Grid, (core, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            tab.Gallery.Switcher.SetHidden(true);
            RenderFrame(window);
            Assert.False(view.IsVisible);

            WindowGalleryOf(view).ExpandedGalleryDisplayMode.Value = GalleryDisplayMode.Masonry;
            RenderFrame(window);

            Assert.False(view.IsVisible);
            Assert.DoesNotContain("tiles", view.Classes);
            Assert.Equal(GalleryDisplayMode.Grid, panel.DisplayMode);

            tab.Gallery.Switcher.SetHidden(false);
            RenderFrame(window);

            Assert.True(view.IsVisible);
            Assert.False(view.IsInAnimation);
            Assert.Contains("tiles", view.Classes);
            Assert.Equal(GalleryDisplayMode.Masonry, panel.DisplayMode);
            Assert.Equal(ScrollBarVisibility.Visible, panel.FindAncestorOfType<ScrollViewer>()!.VerticalScrollBarVisibility);
        });
    }

    [AvaloniaFact]
    public void ExpandedFromClosed_Grid_LocksTheWrapWidthUntilTheAnimationEnds()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Grid, (_, window, view, panel) =>
        {
            ((TabViewModel)view.DataContext!).Gallery.Switcher.ToggleExpanded();
            Dispatcher.UIThread.RunJobs();

            Assert.True(view.IsInAnimation);
            Assert.False(double.IsNaN(panel.WrapWidthOverride));
            WaitForAnimation(window, view);
            Assert.True(double.IsNaN(panel.WrapWidthOverride));
        }, startClosed: true);
    }

    [AvaloniaFact]
    public void ClosingToDocked_ParentResizedDuringTheTween_DoesNotFillTheParentAgain()
    {
        WithShownExpandedGallery(GalleryDisplayMode.Masonry, (_, window, view, _) =>
        {
            ((TabViewModel)view.DataContext!).Gallery.Switcher.ToggleExpanded();
            Dispatcher.UIThread.RunJobs();
            Assert.True(view.IsInAnimation);

            // The parent changes size as the tween reaches the docked extent, as it does in the app when the window fits
            // itself to the docked gallery: the animation has let go of the height, and the look is still Expanded's
            var heights = new List<double>();
            var resized = false;
            view.SizeChanged += (_, e) =>
            {
                if (resized && view.IsInAnimation)
                {
                    heights.Add(e.NewSize.Height);
                }
                else if (!resized && view.IsInAnimation && e.NewSize.Height <= 100.5) // the docked extent here
                {
                    resized = true;
                    window.Height = 598;
                }
            };
            WaitForAnimation(window, view);

            Assert.True(resized);
            Assert.DoesNotContain(heights, height => height > 200);
        });
    }

    // The same window, 1000 by 600, with the tab expanded before its view loads, so the view shows it without an
    // animation. With startClosed the tab is closed instead, for a test that expands it itself
    private static void WithShownExpandedGallery(GalleryDisplayMode displayMode,
        Action<CoreViewModel, Window, GalleryView, VirtualizingGallery> test, double renderScaling = 1,
        bool startClosed = false)
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = displayMode;
        WithShownDockedGallery(test, beforeLoad: (tab, window) =>
        {
            window.Width = 1000;
            window.Height = 600;
            window.SetRenderScaling(renderScaling);
            if (startClosed)
            {
                tab.Gallery.Switcher.Start(docked: false);
            }
            else
            {
                tab.Gallery.Switcher.ToggleExpanded();
            }
        });
    }

    private static ScrollBar VerticalScrollBar(VirtualizingGallery panel) =>
        panel.FindAncestorOfType<ScrollViewer>()!.GetVisualDescendants().OfType<ScrollBar>()
            .Single(bar => bar.Orientation == Orientation.Vertical);

    // The tween runs on the render timer's real time, so tick it until the control is done
    private static void WaitForAnimation(Window window, GalleryView view)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (view.IsInAnimation && DateTime.UtcNow < deadline)
        {
            RenderFrame(window);
            Thread.Sleep(10);
        }
        RenderFrame(window);
        Assert.False(view.IsInAnimation);
    }

    // Shown in a window, so the view loads, with a tab started docked at the bottom and a few items. duringLoad runs
    // as the view loads, when it sizes itself to nothing before it follows the switcher
    private static void WithShownDockedGallery(Action<CoreViewModel, Window, GalleryView, VirtualizingGallery> test,
        bool initializeFirst = true, bool startHidden = false, Action<TabViewModel>? duringLoad = null,
        Action<TabViewModel, Window>? beforeLoad = null)
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        // The display mode saves through the delegate, so the tests that switch it write no settings file
        var core = new CoreViewModel(null, _ => new ImageModel(),
            saveGallerySettings: static () => ValueTask.FromResult(true));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        var tab = vm.WindowTabs.ActiveTab.Value;
        if (initializeFirst)
        {
            tab.Gallery.Initialize(core.GallerySettings);
        }
        tab.Gallery.Switcher.Start(docked: true);
        if (startHidden)
        {
            tab.Gallery.Switcher.SetHidden(true);
        }

        beforeLoad?.Invoke(tab, window);

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView { DataContext = tab };
            view.Initialize(tab);
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            if (duringLoad is not null)
            {
                var ran = false;
                view.PropertyChanged += (_, e) =>
                {
                    if (!ran && view.IsLoaded && e.Property == Layoutable.WidthProperty)
                    {
                        ran = true;
                        duringLoad(tab);
                    }
                };
            }
            window.Content = new DockPanel { Children = { view } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            RenderFrame(window);

            var panel = (VirtualizingGallery)view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!
                .ItemsPanelRoot!;
            test(core, window, view, panel);
        }
        finally
        {
            window.Close();
            application.DataContext = previous;
        }
    }

    private static void RenderFrame(Window window)
    {
        window.InvalidateVisual();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ShownPair_MarksBothItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            // As the image viewer gives it the tab
            viewer.DataContext = tab;
            tab.CurrentWindowSettings.CurrentValue!.IsSideBySide.Value = true;
            tab.ImageIterator!.Initialize([new FileInfo("a.jpg"), new FileInfo("b.jpg"), new FileInfo("c.jpg")]);

            // The step may wait for the cache off the UI thread, so the UI thread runs its jobs until the step ends
            var step = tab.ImageIterator.IterateToIndexAsync(2, new CancellationTokenSource()).AsTask();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!step.IsCompleted && DateTime.UtcNow < deadline)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }
            Dispatcher.UIThread.RunJobs();

            // The last image shows in the pair before it, so the items of both are current
            Assert.True(step.IsCompletedSuccessfully);
            Assert.Equal((1, 2), (viewer.CurrentItemIndex, viewer.PartnerItemIndex));
        });
    }

    private static void WithGalleryView(Action<TabViewModel, NavigateAbleItemsViewer> test) =>
        WithGalleryView(static vm => vm.WindowTabs.ActiveTab.CurrentValue, (tab, _, viewer) => test(tab, viewer));

    private static void WithGalleryView(Func<MainWindowViewModel, TabViewModel> tabOf,
        Action<TabViewModel, MainWindowViewModel, NavigateAbleItemsViewer> test) =>
        WithTab(tabOf, (tab, vm) =>
        {
            var view = new GalleryView();
            view.Initialize(tab);
            test(tab, vm, view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
        });

    private static void WithNavigableView(Action<TabViewModel, NavigableView, NavigateAbleItemsViewer> test) =>
        WithTab(static vm => vm.WindowTabs.ActiveTab.CurrentValue, (tab, _) =>
        {
            tab.SetLoading();
            var nav = (NavigableView)tab.CurrentView.Value!;
            test(tab, nav, nav.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
        });

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel
    private static void WithTab(Func<MainWindowViewModel, TabViewModel> tabOf,
        Action<TabViewModel, MainWindowViewModel> test)
    {
        var core = new CoreViewModel(null, _ => new ImageModel());
        core.InitializeNavigation(
            TestTabServices.For(new SharedImageCache(static f => new ImageModel { FileInfo = f })),
            new NavigationService());
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        vm.WindowTabs.SetViewFactory(new TabViews(window));
        core.MainWindows.ActiveWindow.Value = vm;
        var tab = tabOf(vm);
        tab.Initialize();

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            test(tab, vm);
        }
        finally
        {
            application.DataContext = previous;
            window.Close();
        }
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }



    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    // The gallery settings of the Main Window the view's tab is in
    private static WindowGallerySettings WindowGalleryOf(GalleryView view) =>
        ((TabViewModel)view.DataContext!).ParentWindowContext.WindowSettings.Gallery;
}
