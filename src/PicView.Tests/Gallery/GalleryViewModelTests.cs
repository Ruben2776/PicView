using PicView.Core.ViewModels;
using PicView.Core.Config;
using PicView.Core.Gallery;
using Xunit;
using R3;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class GalleryViewModelTests
{
    private readonly ManualFrameProvider _frameProvider;

    public GalleryViewModelTests()
    {
        _frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = _frameProvider;
        SetDefaults();
    }

    [Fact]
    public void ActiveGalleryMode_WhenChangedFromExpandedToDocked_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel();
        gallery.Initialize();

        gallery.ActiveGalleryMode.Value = GalleryMode.Expanded;
        gallery.SelectedGalleryItemIndex.Value = 5;
        Assert.Equal(5, gallery.SelectedGalleryItemIndex.Value);

        gallery.ActiveGalleryMode.Value = GalleryMode.Docked;
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
    }

    [Fact]
    public void ActiveGalleryMode_WhenChangedFromExpandedToClosed_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel();
        gallery.Initialize();

        gallery.ActiveGalleryMode.Value = GalleryMode.Expanded;
        gallery.SelectedGalleryItemIndex.Value = 3;
        Assert.Equal(3, gallery.SelectedGalleryItemIndex.Value);

        gallery.ActiveGalleryMode.Value = GalleryMode.Closed;
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
    }

    [Fact]
    public void ContractToDockedOrCloseGalleryCommand_WhenExpanded_ResetsSelectedGalleryItemIndex()
    {
        using var gallery = new GalleryViewModel();
        gallery.Initialize();

        gallery.ActiveGalleryMode.Value = GalleryMode.Expanded;
        gallery.SelectedGalleryItemIndex.Value = 4;

        gallery.ContractToDockedOrCloseGalleryCommand.Execute(Unit.Default);
        Assert.Equal(-1, gallery.SelectedGalleryItemIndex.Value);
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