using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.Thumbnailing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.Thumbnailing;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// How Masonry and Rows tiles show what is kept at once and are sharpened with Tile Thumbnails once the view rests,
/// on the real gallery view and the real <see cref="Thumbnails"/>. Time is the test's own: the viewer's rest timer is
/// replaced by one that fires when the test advances its clock past the delay.
/// </summary>
[Collection("Sequential")]
public sealed class GalleryTileSharpeningTests : IDisposable
{
    private readonly BitmapThumbnailDecoder _decoder = new();
    private readonly FakeRestTimer _timer = new();
    private Window? _window;
    private CoreViewModel? _core;
    private TabViewModel? _tab;
    private GalleryView? _view;
    private NavigateAbleItemsViewer? _viewer;
    private VirtualizingGallery? _panel;
    private GalleryItemViewModel[] _items = [];
    private object? _previousDataContext;
    private bool _dataContextSet;

    public GalleryTileSharpeningTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    public void Dispose()
    {
        _decoder.DecodeFullGate?.Set();
        _window?.Close();
        if (_dataContextSet)
        {
            Application.Current!.DataContext = _previousDataContext;
        }
    }

    [AvaloniaFact]
    public void TileRestTimer_InTheApp_IsADispatcherTimerOfTheSharpenDelay()
    {
        var viewer = new NavigateAbleItemsViewer();

        var timer = Assert.IsType<DispatcherTileRestTimer>(viewer.TileRestTimer);
        Assert.Equal(TimeSpan.FromMilliseconds(GalleryDefaults.TileSharpenDelayMilliseconds), timer.Interval);
        Assert.Equal(300, GalleryDefaults.TileSharpenDelayMilliseconds);
    }

    [AvaloniaFact]
    public void Tile_FirstShown_LoadsItsSmallThumbnailAsEveryGalleryItemDoes()
    {
        Show();

        // after the first layout and its jobs, before any rest
        Assert.Equal((int)Thumbnails.GalleryHeight, Shown(0).PixelSize.Height);
        Assert.DoesNotContain(_decoder.Calls, c => c.StartsWith("decode-full:", StringComparison.Ordinal));
        Assert.Equal(0u, Item(0).TileHeightShown);
    }

    [AvaloniaFact]
    public void Tile_WhenTheViewRests_ShowsABitmapThatCoversIt()
    {
        Show();
        Assert.True(Needed(0) > Thumbnails.GalleryHeight);

        Rest();

        Assert.Equal((int)Needed(0), Shown(0).PixelSize.Height);
        Assert.Equal(Needed(0), Item(0).TileHeightShown);
    }

    [AvaloniaFact]
    public void Tile_At150PercentScaling_CoversTheTileInDevicePixels()
    {
        Show(GalleryDisplayMode.Rows, scale: 1.5);

        Rest();

        Assert.Equal(450, Shown(0).PixelSize.Height);
    }

    [AvaloniaFact]
    public void Tile_Sharpened_DoesNotFadeAndIsNeverEmpty()
    {
        // Every small thumbnail is kept, so each tile shows its own at once, before it is arranged
        Show(keepSmallThumbnails: true);
        var image = GalleryImageOf(0);
        var small = Shown(0);
        var emptied = false;
        image.PropertyChanged += (_, e) =>
        {
            if (e.Property == Image.SourceProperty && e.NewValue is null)
            {
                emptied = true;
            }
        };

        Rest();

        Assert.NotSame(small, Shown(0));
        Assert.Same(Shown(0), image.Source);
        Assert.Equal(1, image.Opacity);
        Assert.NotNull(image.Source);
        Assert.False(emptied);
    }

    [AvaloniaFact]
    public void Scrolling_WithoutResting_AsksForNoTileThumbnails()
    {
        Show(itemCount: 60);

        for (var i = 0; i < 5; i++)
        {
            ScrollBy(200);
            AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds - 50);
        }

        Assert.True(ScrollViewer.Offset.Y >= 1000);
        Assert.DoesNotContain(_decoder.Calls, c => c.StartsWith("decode-full:", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void Tile_ScrolledBackAfterSharpening_ShowsTheKeptTileThumbnailAtOnce()
    {
        Show(itemCount: 60);
        Rest();
        var sharp = Shown(0);
        ScrollToEnd();
        Assert.DoesNotContain(_panel!.RealizedItems, x => x.Index == 0);
        Assert.Null(Item(0).Image.Value);
        Assert.Equal(0u, Item(0).TileHeightShown);
        ScrollToStart(); // layout and jobs only, no rest

        Assert.Same(sharp, Shown(0));
        Assert.Equal(1, _decoder.Calls.Count(c => c == $"decode-full:{Item(0).FileInfo!.Name}"));
    }

    [AvaloniaFact]
    public void Tile_DetailsNotRead_IsNotSharpenedUntilTheyAre()
    {
        Show(withoutDetails: [0]);
        Rest();
        Assert.Equal(0u, Item(0).TileHeightShown);

        SetDetails(0, 6000, 4000);
        Rest();

        Assert.True(Needed(0) > 0);
        Assert.Equal(Needed(0), Item(0).TileHeightShown);
    }

    [AvaloniaFact]
    public void SizesArriving_DoNotPostponeSharpening()
    {
        Show(itemCount: 60, withoutDetails: [50, 51, 52]);
        Assert.DoesNotContain(_panel!.RealizedItems, x => x.Index >= 50);

        // every 100 ms, three times: set one off-screen item's details and RaiseSizesChanged, without scrolling
        for (var i = 0; i < 3; i++)
        {
            AdvanceBy(100);
            SetDetails(50 + i, 6000, 4000);
        }
        SettleSharpening();

        Assert.Equal(Needed(0), Item(0).TileHeightShown); // sharp 300 ms after the first layout, not after the last batch
    }

    [AvaloniaFact]
    public void SizesArriving_InRows_DecodeNoSharpTileAgain()
    {
        // Large enough that all twelve tiles are realized
        Show(GalleryDisplayMode.Rows, width: 1900, height: 1100);
        Assert.Contains(_panel!.RealizedItems, x => x.Index == 11);
        Rest();
        var decodes = _decoder.Calls.Count(c => c.StartsWith("decode-full:", StringComparison.Ordinal));

        SetDetails(11, 1000, 4000); // reshuffles the rows
        Rest();

        // only item 11's own
        Assert.Equal(decodes + 1, _decoder.Calls.Count(c => c.StartsWith("decode-full:", StringComparison.Ordinal)));
        Assert.Equal(1, _decoder.Calls.Count(c => c == $"decode-full:{Item(0).FileInfo!.Name}"));
    }

    [AvaloniaFact]
    public void Tile_WhoseTileThumbnailCannotBeMade_KeepsItsSmallOneAndIsNotAskedAgain()
    {
        Show();
        _decoder.CanDecodeFull = false;
        var small = Shown(0);

        Rest();
        // A layout that runs a measure asks for sharpening again, so the second rest fires the timer
        Relayout();
        Assert.True(_timer.IsEnabled);
        Rest();

        Assert.Same(small, Shown(0));
        Assert.Equal(1, _decoder.Calls.Count(c => c == $"decode-full:{Item(0).FileInfo!.Name}"));
    }

    [AvaloniaFact]
    public void ItemSizeRaised_SharpensAgainForTheLargerTile()
    {
        Show();
        Rest();
        var before = Item(0).TileHeightShown;

        RaiseExpandedItemSizeTo(250); // as the item size slider does
        Assert.Equal(before, Item(0).TileHeightShown); // still shown until the rest
        Rest();

        Assert.True(Item(0).TileHeightShown > before);
        Assert.Equal(Needed(0), Item(0).TileHeightShown);
    }

    [AvaloniaFact]
    public void SwitchedToColumns_WhileSharpeningWaits_AsksOnlyForTheGalleryHeight()
    {
        // One slot and the gate closed, so the first tile holds the slot and the others wait for it
        _decoder.DecodeFullGate = new ManualResetEventSlim(false);
        Show(slots: 1);

        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 10);
        SwitchDisplayModeTo(GalleryDisplayMode.Columns);
        OpenGate();
        Rest();

        Assert.True(_decoder.Calls.Count(c => c.StartsWith("decode-full:", StringComparison.Ordinal)) <= 1); // at most the one already in its slot
        // and the one made in its slot lands on no Columns item
        Assert.All(RealizedItems(), item => Assert.Equal(0u, ((GalleryItemViewModel)item.DataContext!).TileHeightShown));
        Assert.DoesNotContain(_items, x => x.Image.Value is Bitmap bitmap && bitmap.PixelSize.Height > Thumbnails.GalleryHeight);
        ScrollToEnd(); // new Columns containers
        Rest();
        Assert.True(_decoder.Calls.Count(c => c.StartsWith("decode-full:", StringComparison.Ordinal)) <= 1);
    }

    [AvaloniaFact]
    public void Tiles_ScrolledOutWhileTheirTileThumbnailsAreMade_ShowNoneOfThemLate()
    {
        // Enough slots that every tile's make is held at the gate, and the new items' small ones still get a slot
        _decoder.DecodeFullGate = new ManualResetEventSlim(false);
        Show(itemCount: 60, slots: 64);
        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 1);
        // The make is in its slot, held at the gate
        Settle(() => _decoder.Calls.Contains($"decode-full:{Item(0).FileInfo!.Name}", StringComparer.Ordinal));

        ScrollToEnd(); // the containers are recycled for other items
        OpenGate();
        SettleSharpening();

        // Item 0 still holds the small thumbnail it showed, as an unloaded item always has
        Assert.Equal((int)Thumbnails.GalleryHeight, Shown(0).PixelSize.Height);
        Assert.Equal(0u, Item(0).TileHeightShown);
        Assert.All(RealizedItems(), item =>
        {
            var vm = (GalleryItemViewModel)item.DataContext!;
            Assert.Equal(0u, vm.TileHeightShown);
            Assert.Equal((int)Thumbnails.GalleryHeight, Assert.IsAssignableFrom<Bitmap>(vm.Image.Value).PixelSize.Height);
        });
    }

    [AvaloniaFact]
    public void Tile_ScrolledALittleWhileItsTileThumbnailIsMade_EndsSharp()
    {
        // Enough slots that every tile's make is in its slot, held at the gate, so a new rest cannot interrupt it
        _decoder.DecodeFullGate = new ManualResetEventSlim(false);
        Show(itemCount: 60, slots: 64);
        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 1);
        Settle(() => _decoder.Calls.Contains($"decode-full:{Item(0).FileInfo!.Name}", StringComparer.Ordinal));

        // A small scroll keeps item 0 realized, and the make outlasts the next rest
        ScrollBy(50);
        Assert.Contains(_panel!.RealizedItems, x => x.Index == 0);
        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 1);
        OpenGate();
        SettleSharpening();

        Assert.Equal(Needed(0), Item(0).TileHeightShown);
        Assert.Equal((int)Needed(0), Shown(0).PixelSize.Height);
        Assert.Equal(1, _decoder.Calls.Count(c => c == $"decode-full:{Item(0).FileInfo!.Name}"));
    }

    [AvaloniaFact]
    public void LeavingExpanded_WhileSharpeningWaits_LandsNoTileThumbnailInTheDockedGallery()
    {
        // One slot and the gate closed, so one make is in its slot and the others wait for it
        _decoder.DecodeFullGate = new ManualResetEventSlim(false);
        Show(slots: 1, dockedFirst: true);
        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 1);
        Settle(() => _decoder.Calls.Any(c => c.StartsWith("decode-full:", StringComparison.Ordinal)));

        _tab!.Gallery.Switcher.ToggleExpanded();
        Settle(() => !_panel!.IsExpanded && !_view!.IsInAnimation);
        OpenGate();
        Rest();

        Assert.Equal(1, _decoder.Calls.Count(c => c.StartsWith("decode-full:", StringComparison.Ordinal)));
        Assert.All(RealizedItems(), item =>
        {
            var vm = (GalleryItemViewModel)item.DataContext!;
            Assert.Equal(0u, vm.TileHeightShown);
            Assert.True(Assert.IsAssignableFrom<Bitmap>(vm.Image.Value).PixelSize.Height <= Thumbnails.GalleryHeight);
        });
    }

    [AvaloniaFact]
    public void DockedGallery_NeverAsksForATileThumbnail()
    {
        // the same items in a docked gallery
        Show(docked: true);

        Rest();

        Assert.Contains(_decoder.Calls, c => c.StartsWith("decode:", StringComparison.Ordinal));
        Assert.DoesNotContain(_decoder.Calls, c => c.StartsWith("decode-full:", StringComparison.Ordinal));
    }

    #region Harness

    /// <summary>
    /// Shows the tab's gallery in a window, expanded in <paramref name="mode"/> or docked at the bottom, with items whose
    /// details are 6000 x 4000 and whose thumbnails come from <see cref="_decoder"/> through the tab's
    /// <see cref="Thumbnails"/>. Returns once the first layout's small thumbnails have landed.
    /// </summary>
    private void Show(GalleryDisplayMode mode = GalleryDisplayMode.Masonry, int itemCount = 12, double scale = 1,
        double width = 800, double height = 600, bool docked = false, int slots = 0, int[]? withoutDetails = null,
        bool keepSmallThumbnails = false, bool dockedFirst = false)
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        Settings.Gallery.ExpandedGalleryDisplayMode = mode;
        var cache = new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
        var thumbnails = new Thumbnails(_decoder, cache, slots);
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            saveGallerySettings: static () => ValueTask.FromResult(true));
        _core.InitializeNavigation(TestTabServices.For(cache, thumbnails), new NavigationService(string.CompareOrdinal));
        _window = new TestMainWindow(new NoOpModeAdapter()) { Width = width, Height = height };
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings);
        _window.DataContext = vm;
        _tab = vm.WindowTabs.ActiveTab.Value;
        _tab.Initialize();
        _tab.Gallery.Initialize(_core.GallerySettings);
        if (docked)
        {
            _tab.Gallery.Switcher.Start(docked: true);
        }
        else
        {
            // The Expanded Gallery reached without animation: expanded and hidden before the view loads, then shown.
            // Expanded from docked, toggling it again docks it
            if (dockedFirst)
            {
                _tab.Gallery.Switcher.Start(docked: true);
            }

            _tab.Gallery.Switcher.ToggleExpanded();
            _tab.Gallery.Switcher.SetHidden(true);
        }

        var application = Application.Current!;
        _previousDataContext = application.DataContext;
        application.DataContext = _core;
        _dataContextSet = true;

        var view = new GalleryView { DataContext = _tab };
        _view = view;
        view.Initialize(_tab);
        _viewer = view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!;
        _viewer.TileRestTimer = _timer;

        _items = Enumerable.Range(0, itemCount).Select(i => new GalleryItemViewModel
        {
            FileInfo = new FileInfo($"{i:D4}.jpg"),
            Coordinator = _tab.GalleryCoordinator,
            ThumbnailHeight = Thumbnails.GalleryHeight
        }).ToArray();
        for (var i = 0; i < itemCount; i++)
        {
            if (withoutDetails is null || !withoutDetails.Contains(i))
            {
                _items[i].SetDetails(new GalleryFileDetails(6000, 4000, null));
            }

            if (keepSmallThumbnails)
            {
                thumbnails.GetAsync(_items[i].FileInfo!, Thumbnails.GalleryHeight, _tab.Id,
                    TestContext.Current.CancellationToken).AsTask().GetAwaiter().GetResult();
            }
        }

        _tab.Gallery.GalleryItems.AddRange(_items);
        _window.Content = new DockPanel { Children = { view } };
        if (scale != 1)
        {
            _window.SetRenderScaling(scale);
        }

        _window.Show();
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
        if (!docked)
        {
            _tab.Gallery.Switcher.SetHidden(false);
            Dispatcher.UIThread.RunJobs();
        }

        _panel = (VirtualizingGallery)_viewer.ItemsPanelRoot!;
        for (var frame = 0; frame < 10 && _panel.RealizedItems.Count is 0; frame++)
        {
            RenderFrame();
        }

        Assert.NotEmpty(_panel.RealizedItems);
        Assert.Equal(!docked, _panel.IsExpanded);
        SettleSmallThumbnails();
    }

    /// <summary>Advances the test's clock past the sharpen delay, then waits until the decodes it started landed.</summary>
    private void Rest()
    {
        AdvanceBy(GalleryDefaults.TileSharpenDelayMilliseconds + 1);
        SettleSharpening();
    }

    /// <summary>
    /// Advances the test's clock, fires the rest timer if it is due by then, and runs the jobs until the small
    /// thumbnails of the realized items have landed. Sharpening it started may still be running.
    /// </summary>
    private void AdvanceBy(int milliseconds)
    {
        _timer.Now += milliseconds;
        if (_timer.IsEnabled && _timer.Due <= _timer.Now)
        {
            _viewer!.OnTileRestElapsed();
        }

        SettleSmallThumbnails();
    }

    private void SettleSmallThumbnails() => Settle(() => RealizedItems().All(ShowsItsImage));

    private void SettleSharpening() => Settle(() =>
        _decoder.InFlight is 0 && RealizedItems().All(x => !x.IsSharpening && ShowsItsImage(x)));

    private void Settle(Func<bool> isSettled)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            Dispatcher.UIThread.RunJobs();
            RenderFrame();
            if (isSettled())
            {
                // Once more, so a post the last landing made has run too
                Dispatcher.UIThread.RunJobs();
                RenderFrame();
                if (isSettled())
                {
                    return;
                }
            }

            Assert.True(DateTime.UtcNow < deadline, "The gallery did not settle");
            Thread.Sleep(1);
        }
    }

    private static bool ShowsItsImage(GalleryItem item) =>
        item.DataContext is GalleryItemViewModel { Image.Value: { } image } &&
        ReferenceEquals(item.FindControl<ThumbImage>("GalleryImage")!.Source, image);

    private List<GalleryItem> RealizedItems()
    {
        var items = new List<GalleryItem>();
        foreach (var realized in _panel!.RealizedItems)
        {
            if (realized.Element is ContentPresenter { Child: GalleryItem item })
            {
                items.Add(item);
            }
        }

        return items;
    }

    private GalleryItemViewModel Item(int index) => _items[index];

    private Bitmap Shown(int index) => Assert.IsAssignableFrom<Bitmap>(Item(index).Image.Value);

    private uint Needed(int index) => _panel!.TileThumbnailHeightFor(index);

    private GalleryItem ItemAt(int index) => Assert.IsType<GalleryItem>(
        Assert.IsType<ContentPresenter>(_panel!.RealizedItems.Single(x => x.Index == index).Element).Child);

    private ThumbImage GalleryImageOf(int index) => ItemAt(index).FindControl<ThumbImage>("GalleryImage")!;

    private ScrollViewer ScrollViewer => _panel!.FindAncestorOfType<ScrollViewer>()!;

    /// <summary>Scrolls as the user does, then runs the layout and its jobs.</summary>
    private void ScrollBy(double y)
    {
        ScrollTo(ScrollViewer.Offset.WithY(ScrollViewer.Offset.Y + y));
    }

    private void ScrollToEnd()
    {
        var scrollViewer = ScrollViewer;
        ScrollTo(new Vector(Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width),
            Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height)));
    }

    private void ScrollToStart() => ScrollTo(default);

    private void ScrollTo(Vector offset)
    {
        ScrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, offset);
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
        SettleSmallThumbnails();
    }

    /// <summary>Gives an item its details as the gallery's reader does, and tells the view sizes arrived.</summary>
    private void SetDetails(int index, uint width, uint height)
    {
        Item(index).SetDetails(new GalleryFileDetails(width, height, null));
        _tab!.Gallery.RaiseSizesChanged();
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
        SettleSmallThumbnails();
    }

    private void Relayout()
    {
        _panel!.InvalidateMeasure();
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
    }

    private void RaiseExpandedItemSizeTo(double size)
    {
        _core!.GallerySettings.ExpandedGalleryItemSize.Value = size;
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
        SettleSmallThumbnails();
        Assert.Equal(size, _panel!.ItemHeight);
    }

    // Runs only the jobs: the small thumbnails of new containers may wait behind the held slot
    private void SwitchDisplayModeTo(GalleryDisplayMode mode)
    {
        _core!.GallerySettings.SetExpandedDisplayModeCommand.Execute(mode);
        Dispatcher.UIThread.RunJobs();
        RenderFrame();
        Assert.Equal(mode, _panel!.DisplayMode);
    }

    private void OpenGate() => _decoder.DecodeFullGate!.Set();

    private void RenderFrame()
    {
        _window!.InvalidateVisual();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// The viewer's rest timer on the test's clock: <see cref="Start"/> sets it due a sharpen delay from now, and the
    /// test fires it once it advances its clock that far.
    /// </summary>
    private sealed class FakeRestTimer : ITileRestTimer
    {
        public long Now { get; set; }
        public long Due { get; private set; }
        public bool IsEnabled { get; private set; }

        public void Start()
        {
            if (IsEnabled)
            {
                return;
            }

            IsEnabled = true;
            Due = Now + GalleryDefaults.TileSharpenDelayMilliseconds;
        }

        public void Stop() => IsEnabled = false;
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

    #endregion
}
