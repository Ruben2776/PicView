using PicView.Core.ViewModels;
using PicView.Core.Config;
using PicView.Core.Gallery;
using Xunit;
using R3;

namespace PicView.Tests.Gallery;

public class GalleryViewModelTests
{
    private readonly ManualFrameProvider _frameProvider;

    public GalleryViewModelTests()
    {
        _frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = _frameProvider;
        SettingsManager.SetDefaults();
    }

    [Fact]
    public void IsGalleryDocked_Change_Updates_GalleryMode()
    {
        using var vm = new GalleryViewModel();
        
        // Ensure Initial state is Closed
        vm.ActiveGalleryMode.Value = GalleryMode2.Closed;
        Settings.Gallery.IsGalleryDocked = false;
        _frameProvider.Tick(); // Propagate initial false if any
        
        // Change Setting to true
        Settings.Gallery.IsGalleryDocked = true;
        _frameProvider.Tick();
        
        // Expect Docked
        Assert.Equal(GalleryMode2.Docked, vm.ActiveGalleryMode.Value);
        
        // Change Setting to false
        Settings.Gallery.IsGalleryDocked = false;
        _frameProvider.Tick();
        
        // Expect Closed
        Assert.Equal(GalleryMode2.Closed, vm.ActiveGalleryMode.Value);
    }

    [Fact]
    public void DockPosition_Change_Forces_Docked()
    {
        using var vm = new GalleryViewModel();
        
        // Initial state (Closed)
        Settings.Gallery.IsGalleryDocked = false;
        vm.ActiveGalleryMode.Value = GalleryMode2.Closed;
        _frameProvider.Tick();
        
        // Change Position (different from default Bottom)
        // Note: Default might be Bottom. Ensure we change it.
        Settings.Gallery.DockPosition = GalleryDockPosition.Left;
        _frameProvider.Tick();
        
        // Expect Docked and IsGalleryDocked=true
        Assert.Equal(GalleryMode2.Docked, vm.ActiveGalleryMode.Value);
        Assert.True(Settings.Gallery.IsGalleryDocked);
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
