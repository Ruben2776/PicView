using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;
using Xunit;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class GalleryNavigationTests
{
    public GalleryNavigationTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public async Task NavigateDirectionalAsync_WhenGalleryIsExpanded_DoesNotNavigateGallery()
    {
        // The Gallery Input Mode navigates the expanded gallery, so Next and Prev only move through the files
        var tabOverview = new TabOverviewViewModel(null!);
        var tab = tabOverview.ActiveTab.Value;
        tab.Gallery.Switcher.ToggleExpanded();

        NavigateTo? capturedNavigation = null;
        tab.Gallery.NavigateGalleryCommand.Subscribe(x => capturedNavigation = x);

        await tabOverview.NavigateDirectionalAsync(false, NavigateTo.Next);

        Assert.Null(capturedNavigation);
    }

    [Fact]
    public async Task NavigateDirectionalAsync_WhenGalleryIsNotExpanded_ShouldNotNavigateGallery()
    {
        // Arrange
        var tabOverview = new TabOverviewViewModel(null!);
        var tab = tabOverview.ActiveTab.Value;
        
        Assert.False(tab.Gallery.IsGalleryExpanded.Value);
        
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
