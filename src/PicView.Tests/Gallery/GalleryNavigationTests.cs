using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;
using Xunit;

namespace PicView.Tests.Gallery;

public class GalleryNavigationTests
{
    public GalleryNavigationTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public async Task NavigateDirectionalAsync_WhenGalleryIsExpanded_ShouldNavigateGallery()
    {
        // Arrange
        var tabOverview = new TabOverviewViewModel();
        var tab = tabOverview.ActiveTab.Value;
        
        tab.Gallery.IsGalleryExpanded.Value = true;
        
        NavigateTo? capturedNavigation = null;
        tab.Gallery.NavigateGalleryCommand.Subscribe(x => capturedNavigation = x);

        // Act
        await tabOverview.NavigateDirectionalAsync(false, NavigateTo.Next);

        // Assert
        Assert.NotNull(capturedNavigation);
        Assert.Equal(NavigateTo.Next, capturedNavigation);
    }
    
    [Fact]
    public async Task NavigateDirectionalAsync_WhenGalleryIsExpanded_ShouldNavigateGallery_Previous()
    {
        // Arrange
        var tabOverview = new TabOverviewViewModel();
        var tab = tabOverview.ActiveTab.Value;
        
        tab.Gallery.IsGalleryExpanded.Value = true;
        
        NavigateTo? capturedNavigation = null;
        tab.Gallery.NavigateGalleryCommand.Subscribe(x => capturedNavigation = x);

        // Act
        await tabOverview.NavigateDirectionalAsync(false, NavigateTo.Previous);

        // Assert
        Assert.NotNull(capturedNavigation);
        Assert.Equal(NavigateTo.Previous, capturedNavigation);
    }

    [Fact]
    public async Task NavigateDirectionalAsync_WhenGalleryIsNotExpanded_ShouldNotNavigateGallery()
    {
        // Arrange
        var tabOverview = new TabOverviewViewModel();
        var tab = tabOverview.ActiveTab.Value;
        
        tab.Gallery.IsGalleryExpanded.Value = false;
        
        bool wasCalled = false;
        tab.Gallery.NavigateGalleryCommand.Subscribe(x => wasCalled = true);

        // Act
        // This might return early because SharedNavigation/ImageIterator is null,
        // which is fine, we just want to prove it didn't call Gallery Navigate.
        await tabOverview.NavigateDirectionalAsync(false, NavigateTo.Next);

        // Assert
        Assert.False(wasCalled);
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
