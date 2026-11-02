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
        var shared = new GallerySharedSettingsViewModel();
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        gallery.Switcher.Start(true);

        shared.DockedGalleryItemSize.Value = 120;

        Assert.Equal(120, gallery.Appearance.Value.ItemHeight);
    }

    [Fact]
    public void Appearance_ExpandedStretchChanged_WhileDocked_KeepsDockedStretch()
    {
        var shared = new GallerySharedSettingsViewModel();
        shared.DockedGalleryStretchMode.Value = GalleryStretchMode.Uniform;
        using var gallery = new GalleryViewModel(NoSave);
        gallery.Initialize(shared);
        gallery.Switcher.Start(true);

        shared.ExpandedGalleryStretchMode.Value = GalleryStretchMode.FillSquare;

        Assert.Equal(GalleryStretchMode.Uniform, gallery.Appearance.Value.Stretch);
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
