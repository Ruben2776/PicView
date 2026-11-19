using PicView.Core.ViewModels;
using PicView.Core.Gallery;
using Xunit;
using R3;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class GalleryViewModelTests
{
    private readonly ManualFrameProvider _frameProvider;
    private static readonly Func<ValueTask<bool>> NoSave = static () => ValueTask.FromResult(true);

    public GalleryViewModelTests()
    {
        _frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = _frameProvider;
        SetDefaults();
    }

    [Fact]
    public void ShownMode_WhenChangedFromExpandedToDocked_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        gallery.Switcher.Start(true);

        gallery.Switcher.ToggleExpanded();
        gallery.SelectedGalleryItemIndex.Value = 5;
        Assert.Equal(5, gallery.SelectedGalleryItemIndex.Value);

        gallery.Switcher.ToggleExpanded();
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
    }

    [Fact]
    public async Task ShownMode_WhenChangedFromExpandedToClosed_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());

        gallery.Switcher.ToggleExpanded();
        gallery.SelectedGalleryItemIndex.Value = 3;
        Assert.Equal(3, gallery.SelectedGalleryItemIndex.Value);

        await gallery.Switcher.Close();
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
    }

    [Fact]
    public void ContractCommand_WhenExpanded_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());

        gallery.Switcher.ToggleExpanded();
        gallery.SelectedGalleryItemIndex.Value = 4;

        gallery.ContractCommand.Execute(Unit.Default);
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
    }

    [Fact]
    public void DockFlags_WhenDockedAtLeft_OnlyLeftIsTrue()
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Left;
        using var gallery = new GalleryViewModel(NoSave);

        gallery.Switcher.Start(true);

        Assert.True(gallery.IsLeftDocked.Value);
        Assert.False(gallery.IsRightDocked.Value);
        Assert.False(gallery.IsTopDocked.Value);
        Assert.False(gallery.IsBottomDocked.Value);
    }

    [Fact]
    public void DockFlags_WhenHidden_AllFalse()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Switcher.Start(true);

        gallery.Switcher.SetHidden(true);

        Assert.False(gallery.IsLeftDocked.Value);
        Assert.False(gallery.IsRightDocked.Value);
        Assert.False(gallery.IsTopDocked.Value);
        Assert.False(gallery.IsBottomDocked.Value);
        Assert.False(gallery.IsDockedGalleryVisible.Value);
    }

    [Fact]
    public async Task Appearance_FollowsTheShownMode()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());

        gallery.Switcher.Start(true);
        Assert.Equal(GalleryMode.Docked, gallery.Appearance.Value.Mode);

        gallery.Switcher.ToggleExpanded();
        Assert.Equal(GalleryMode.Expanded, gallery.Appearance.Value.Mode);

        await gallery.Switcher.Close();
        Assert.Equal(GalleryMode.Closed, gallery.Appearance.Value.Mode);
    }

    [Fact]
    public void Appearance_HiddenGallery_IsClosed()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        gallery.Switcher.Start(true);

        gallery.Switcher.SetHidden(true);
        Assert.Equal(GalleryMode.Closed, gallery.Appearance.Value.Mode);

        gallery.Switcher.SetHidden(false);
        Assert.Equal(GalleryMode.Docked, gallery.Appearance.Value.Mode);
    }

    [Fact]
    public void Appearance_DockPositionChanged_FollowsIt()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        gallery.Switcher.Start(true);

        Settings.Gallery.DockPosition = GalleryDockPosition.Left;
        _frameProvider.Tick();

        Assert.Equal(GalleryDockPosition.Left, gallery.Appearance.Value.Dock);
        Assert.Equal(GalleryOrientation.Vertical, gallery.Appearance.Value.Orientation);
    }

    [Fact]
    public async Task Appearance_DockAtNewEdgeWhileClosed_HasTheNewEdgeWhenTheChangeIsPublished()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        gallery.Switcher.Start(true);
        await gallery.Switcher.Close();

        GalleryDockPosition? dockWhenPublished = null;
        using var _ = gallery.Switcher.Changes.Subscribe(_ => dockWhenPublished = gallery.Appearance.Value.Dock);

        // No frame passes, so the dock position poll hasn't seen the new edge yet
        await gallery.Switcher.DockAt(GalleryDockPosition.Left);

        Assert.Equal(GalleryDockPosition.Left, dockWhenPublished);
        Assert.Equal(GalleryMode.Docked, gallery.Appearance.Value.Mode);
    }

    [Fact]
    public void Appearance_DockedItemSizeChanged_FollowsIt()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        var window = FollowedWindow(gallery);
        gallery.Switcher.Start(true);

        window.DockedGalleryItemSize.Value = 120;

        Assert.Equal(120, gallery.Appearance.Value.ItemHeight);
    }

    [Fact]
    public void Appearance_ExpandedStretchChanged_WhileDocked_KeepsDockedStretch()
    {
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(new GallerySharedSettingsViewModel());
        var window = FollowedWindow(gallery);
        window.DockedGalleryStretchMode.Value = GalleryStretchMode.Uniform;
        gallery.Switcher.Start(true);

        window.ExpandedGalleryStretchMode.Value = GalleryStretchMode.FillSquare;

        Assert.Equal(GalleryStretchMode.Uniform, gallery.Appearance.Value.Stretch);
    }

    [Fact]
    public void Initialize_SharedItemSpacingChanged_AppearanceTakesItAtOnce()
    {
        var shared = new GallerySharedSettingsViewModel(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        gallery.Switcher.ToggleExpanded();

        shared.GalleryItemSpacing.Value = 12;

        Assert.Equal(12, gallery.Appearance.Value.ItemSpacing);
    }

    [Fact]
    public void Initialize_SharedLineSpacingChanged_LineSpacingTakesItAtOnce()
    {
        var shared = new GallerySharedSettingsViewModel(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);

        shared.GalleryLineSpacing.Value = 9;

        Assert.Equal(9, gallery.LineSpacing.Value);
    }

    [Fact]
    public void Initialize_TwoTabsOneOpenedAfterAChange_BothTakeTheSharedSpacing()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var first = new GalleryViewModel(NoSave);
        first.Initialize(shared);
        first.Switcher.ToggleExpanded();
        shared.GalleryItemSpacing.Value = 12;
        using var second = new GalleryViewModel(NoSave);
        second.Initialize(shared);
        second.Switcher.ToggleExpanded();

        Assert.Equal(12, first.Appearance.Value.ItemSpacing);
        Assert.Equal(12, second.Appearance.Value.ItemSpacing);
    }

    [Fact]
    public void Initialize_TabOpenedAfterStartAndASpacingIsRefused_TakesTheAcceptedSpacing()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        gallery.Switcher.ToggleExpanded();

        shared.GalleryItemSpacing.Value = 0;
        shared.GalleryLineSpacing.Value = 3;

        Assert.Equal(5, gallery.Appearance.Value.ItemSpacing);
        Assert.Equal(8, gallery.LineSpacing.Value);
    }

    [Fact]
    public void Appearance_DisplayModeChanged_WhileExpanded_FollowsIt()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        var window = FollowedWindow(gallery);
        gallery.Switcher.ToggleExpanded();
        Assert.Equal(GalleryDisplayMode.Columns, gallery.Appearance.Value.DisplayMode);

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        Assert.Equal(GalleryDisplayMode.Masonry, gallery.Appearance.Value.DisplayMode);
        Assert.False(gallery.Appearance.Value.ScrollsHorizontally);

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        Assert.Equal(GalleryDisplayMode.Rows, gallery.Appearance.Value.DisplayMode);

        window.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Columns, gallery.Appearance.Value.DisplayMode);
        Assert.True(gallery.Appearance.Value.ScrollsHorizontally);
    }

    [Fact]
    public void Appearance_DisplayModeChanged_WhileDocked_PublishesNothingUntilExpanded()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        var window = FollowedWindow(gallery);
        gallery.Switcher.Start(true);
        var docked = gallery.Appearance.Value;
        var published = 0;
        using var subscription = gallery.Appearance.AsObservable().Skip(1).Subscribe(_ => published++);

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);

        Assert.Equal(0, published);
        Assert.Equal(docked, gallery.Appearance.Value);

        gallery.Switcher.ToggleExpanded();
        Assert.Equal(GalleryDisplayMode.Masonry, gallery.Appearance.Value.DisplayMode);

        gallery.Switcher.ToggleExpanded();
        Assert.Equal(docked, gallery.Appearance.Value);
    }

    [Fact]
    public void Appearance_DisplayModeChanged_ReachesEveryExpandedTab()
    {
        // One window's settings serve every tab of that window
        var shared = StartedGallerySettings.New(NoSave);
        using var first = new GalleryViewModel(NoSave);
        using var second = new GalleryViewModel(NoSave);
        first.Initialize(shared);
        second.Initialize(shared);
        var window = FollowedWindow(first, second);
        first.Switcher.ToggleExpanded();
        second.Switcher.ToggleExpanded();

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);

        Assert.Equal(GalleryDisplayMode.Rows, first.Appearance.Value.DisplayMode);
        Assert.Equal(GalleryDisplayMode.Rows, second.Appearance.Value.DisplayMode);
        Assert.True(window.IsExpandedDisplayModeRows.Value);
    }

    [Fact]
    public void Appearance_DisplayModeChanged_WhileHidden_PublishesNothingAndComesBackInTheNewMode()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        var window = FollowedWindow(gallery);
        gallery.Switcher.ToggleExpanded();
        // As crop, a Single Image or the Gallery File Limit hides it (Review Focus 5)
        gallery.Switcher.SetHidden(true);
        var hidden = gallery.Appearance.Value;
        var published = 0;
        using var subscription = gallery.Appearance.AsObservable().Skip(1).Subscribe(_ => published++);

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);

        Assert.Equal(0, published);
        Assert.Equal(hidden, gallery.Appearance.Value);

        gallery.Switcher.SetHidden(false);
        Assert.Equal(GalleryDisplayMode.Rows, gallery.Appearance.Value.DisplayMode);
        Assert.False(gallery.Appearance.Value.ScrollsHorizontally);
    }

    // Two tabs of one Main Window: each has its own GalleryViewModel, and both follow the window's settings
    [Fact]
    public void Appearance_DisplayModeChanged_TwoExpandedTabsSharingTheSettings_BothFollowIt()
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var first = new GalleryViewModel(NoSave);
        using var second = new GalleryViewModel(NoSave);
        first.Initialize(shared);
        second.Initialize(shared);
        var window = FollowedWindow(first, second);
        first.Switcher.ToggleExpanded();
        second.Switcher.ToggleExpanded();

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);

        Assert.Equal(GalleryDisplayMode.Masonry, first.Appearance.Value.DisplayMode);
        Assert.False(first.Appearance.Value.ScrollsHorizontally);
        Assert.Equal(first.Appearance.Value, second.Appearance.Value);
        // What both tabs' switch buttons and checked menu items read
        Assert.True(window.IsExpandedDisplayModeMasonry.Value);
        Assert.False(window.IsExpandedDisplayModeColumns.Value);

        window.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

        Assert.Equal(GalleryDisplayMode.Columns, first.Appearance.Value.DisplayMode);
        Assert.True(first.Appearance.Value.ScrollsHorizontally);
        Assert.Equal(first.Appearance.Value, second.Appearance.Value);
        Assert.True(window.IsExpandedDisplayModeColumns.Value);
    }

    // Review Focus 5. Crop and a Single Image hide the gallery with SetHidden, the Gallery File Limit with SetOverLimit.
    // The mode stays Expanded and the shown mode is Closed
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Appearance_DisplayModeChanged_WhileAnExpandedGalleryIsHidden_PublishesNothingUntilItIsShownAgain(
        bool byFileLimit)
    {
        var shared = StartedGallerySettings.New(NoSave);
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        var window = FollowedWindow(gallery);
        gallery.Switcher.ToggleExpanded();
        Action<bool> hide = byFileLimit ? gallery.Switcher.SetOverLimit : gallery.Switcher.SetHidden;
        hide(true);
        Assert.Equal(GalleryMode.Expanded, gallery.Mode.Value);
        Assert.Equal(GalleryMode.Closed, gallery.ShownMode.Value);
        var whileHidden = gallery.Appearance.Value;
        Assert.Equal(GalleryMode.Closed, whileHidden.Mode);
        Assert.Equal(GalleryDisplayMode.Columns, whileHidden.DisplayMode);
        var published = 0;
        using var subscription = gallery.Appearance.AsObservable().Skip(1).Subscribe(_ => published++);

        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        window.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);

        Assert.Equal(0, published);
        Assert.Equal(whileHidden, gallery.Appearance.Value);

        hide(false);

        // Shown again: one new appearance, Expanded, in the display mode picked last while it was hidden
        Assert.Equal(1, published);
        Assert.Equal(GalleryMode.Expanded, gallery.Appearance.Value.Mode);
        Assert.Equal(GalleryDisplayMode.Rows, gallery.Appearance.Value.DisplayMode);
        Assert.False(gallery.Appearance.Value.ScrollsHorizontally);
    }

    [Fact]
    public void Appearance_StoredDisplayMode_AppliesBeforeAndAfterInitialize()
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = GalleryDisplayMode.Rows;
        using var gallery = new GalleryViewModel(NoSave);

        gallery.Switcher.ToggleExpanded();
        Assert.Equal(GalleryDisplayMode.Rows, gallery.Appearance.Value.DisplayMode);

        gallery.Initialize(new GallerySharedSettingsViewModel(NoSave));
        Assert.Equal(GalleryDisplayMode.Rows, gallery.Appearance.Value.DisplayMode);
    }

    [Fact]
    public void Gallery_WithoutAWindow_UsesTheRecordsLook()
    {
        Settings.Gallery.DockedGalleryStretchMode = GalleryStretchMode.FillSquare;
        Settings.Gallery.DockedGalleryItemSize = 120;
        Settings.Gallery.ExpandedGalleryStretchMode = GalleryStretchMode.Square;
        Settings.Gallery.ExpandedGalleryItemSize = 200;
        using var tab = new TabViewModel(null!);
        using var standalone = new GalleryViewModel(NoSave);

        foreach (var gallery in new[] { tab.Gallery, standalone })
        {
            // Before Initialize, as a tab can start docked
            gallery.Switcher.Start(true);
            Assert.Equal(GalleryStretchMode.FillSquare, gallery.Appearance.Value.Stretch);
            Assert.Equal(120, gallery.Appearance.Value.ItemHeight);

            gallery.Initialize(StartedGallerySettings.New(NoSave));
            Assert.Equal(GalleryStretchMode.FillSquare, gallery.Appearance.Value.Stretch);
            Assert.Equal(120, gallery.Appearance.Value.ItemHeight);

            gallery.Switcher.ToggleExpanded();
            Assert.Equal(GalleryStretchMode.Square, gallery.Appearance.Value.Stretch);
            Assert.Equal(200, gallery.Appearance.Value.ItemHeight);
        }
    }

    // A window's gallery settings, started as an opened window's are, which each gallery follows as a tab in that
    // window does
    private static WindowGallerySettings FollowedWindow(params GalleryViewModel[] galleries)
    {
        var settings = WindowGallerySettings.FromRecord(Settings.Gallery);
        settings.Start();
        foreach (var gallery in galleries)
        {
            gallery.FollowLook(settings.Look);
        }

        return settings;
    }

    private class ManualFrameProvider : FrameProvider
    {
        private readonly List<IFrameRunnerWorkItem> _items = new();

        public override long GetFrameCount() => 0;

        public override void Register(IFrameRunnerWorkItem callback)
        {
            _items.Add(callback);
        }

        public void Tick()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!item.MoveNext(0))
                {
                    _items.RemoveAt(i);
                }
            }
        }
    }

}
