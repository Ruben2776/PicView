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
        gallery.Initialize();
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
        gallery.Initialize();

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
        gallery.Initialize();

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
