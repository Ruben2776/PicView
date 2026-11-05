using PicView.Avalonia.Input;
using PicView.Core.IPlatform;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Input;

[Collection("Sequential")]
public class ContextualActionHandlerTests
{

    private sealed class FakeCropService : ICropService
    {
        public List<string> Calls { get; } = [];

        public bool IsCropping => true;

        public Task StartCropControlAsync() => Task.CompletedTask;

        public Task SaveCropAsAsync()
        {
            Calls.Add(nameof(SaveCropAsAsync));
            return Task.CompletedTask;
        }

        public ValueTask CopyCroppedImageAsync()
        {
            Calls.Add(nameof(CopyCroppedImageAsync));
            return ValueTask.CompletedTask;
        }

        public void CloseCropControl() => Calls.Add(nameof(CloseCropControl));

        public object? GetCroppedImage() => null;
    }

    private sealed class FakeScrollTarget(bool isImageTallerThanViewer) : IZoomScrollTarget
    {
        public bool IsImageTallerThanViewer => isImageTallerThanViewer;
        public bool CanScrollVertically => isImageTallerThanViewer;
        public void ScrollLine(bool down) { }
        public void ScrollToHome() { }
        public void ScrollToEnd() { }
        public void ZoomIn() { }
        public void ZoomOut() { }
        public void ZoomAt(bool zoomIn, global::Avalonia.Point pointer) { }
    }

    private static (ContextualActionHandler Handler, FakeCropService Crop) CreateHandler() =>
        CreateHandler(out _);

    private static (ContextualActionHandler Handler, FakeCropService Crop) CreateHandler(out TabViewModel tab)
    {
        SetDefaults();
        var vm = new MainWindowViewModel(null!, null!, null!, null!);
        var crop = new FakeCropService();
        tab = new TabViewModel(vm) { CropService = crop };
        vm.WindowTabs.ActiveTab.Value = tab;

        // The crop and gallery meanings only use the view model, not the window
        return (new ContextualActionHandler(null!, vm), crop);
    }

    [Theory]
    // Confirm asks where to save, as cropping from the context menu does, instead of overwriting the image
    [InlineData(ContextualActionHandler.Confirm, nameof(ICropService.SaveCropAsAsync))]
    [InlineData(ContextualActionHandler.Cancel, nameof(ICropService.CloseCropControl))]
    [InlineData(ContextualActionHandler.Copy, nameof(ICropService.CopyCroppedImageAsync))]
    public async Task Crop_ContextualAction_RunsItsCropOperation(string action, string expectedCall)
    {
        var (handler, crop) = CreateHandler();

        var claimed = await handler.TryRunAsync(InputMode.Crop, action);

        Assert.True(claimed);
        Assert.Equal([expectedCall], crop.Calls);
    }

    [Fact]
    public async Task Crop_PlayPause_IsNotClaimed()
    {
        var (handler, crop) = CreateHandler();

        Assert.False(await handler.TryRunAsync(InputMode.Crop, ContextualActionHandler.PlayPause));
        Assert.Empty(crop.Calls);
    }

    [Theory]
    [InlineData(ContextualActionHandler.Up, NavigateTo.Up)]
    [InlineData(ContextualActionHandler.Down, NavigateTo.Down)]
    [InlineData(ContextualActionHandler.Left, NavigateTo.Previous)]
    [InlineData(ContextualActionHandler.Right, NavigateTo.Next)]
    public async Task Gallery_Arrow_NavigatesGallery(string action, NavigateTo expected)
    {
        var (handler, _) = CreateHandler(out var tab);
        tab.Gallery.Switcher.ToggleExpanded();
        NavigateTo? navigated = null;
        using var subscription = tab.Gallery.NavigateGalleryCommand.Subscribe(direction => navigated = direction);

        var claimed = await handler.TryRunAsync(InputMode.Gallery, action);

        Assert.True(claimed);
        Assert.Equal(expected, navigated);
    }

    [Fact]
    public async Task Gallery_Cancel_CollapsesGallery()
    {
        var (handler, _) = CreateHandler(out var tab);
        tab.Gallery.Switcher.ToggleExpanded();
        Assert.True(tab.Gallery.IsGalleryExpanded.Value);

        var claimed = await handler.TryRunAsync(InputMode.Gallery, ContextualActionHandler.Cancel);

        Assert.True(claimed);
        Assert.False(tab.Gallery.IsGalleryExpanded.Value);
    }

    [Fact]
    public async Task Gallery_PlayPause_IsNotClaimed()
    {
        var (handler, _) = CreateHandler(out var tab);
        tab.Gallery.Switcher.ToggleExpanded();

        Assert.False(await handler.TryRunAsync(InputMode.Gallery, ContextualActionHandler.PlayPause));
    }

    [Theory]
    [InlineData(ContextualActionHandler.Left)]
    [InlineData(ContextualActionHandler.Right)]
    public async Task Normal_LeftOrRight_IsNotClaimed(string action)
    {
        var (handler, _) = CreateHandler();

        Assert.False(await handler.TryRunAsync(InputMode.Normal, action));
    }

    [Theory]
    [InlineData(ContextualActionHandler.Up)]
    [InlineData(ContextualActionHandler.Down)]
    public async Task Normal_UpOrDownWithoutImageViewer_IsNotClaimed(string action)
    {
        // Nothing to scroll, so the Keybind falls through to rotating
        var (handler, _) = CreateHandler();

        Assert.False(await handler.TryRunAsync(InputMode.Normal, action));
    }

    [Fact]
    public void ScrollsImage_ScrollingOnAndTaller_IsTrue()
    {
        SetDefaults();
        Settings.Zoom.ScrollEnabled = true;

        Assert.True(ContextualActionHandler.ScrollsImage(new FakeScrollTarget(isImageTallerThanViewer: true)));
    }

    [Fact]
    public void ScrollsImage_ScrollingOnAndFits_IsFalse()
    {
        SetDefaults();
        Settings.Zoom.ScrollEnabled = true;

        Assert.False(ContextualActionHandler.ScrollsImage(new FakeScrollTarget(isImageTallerThanViewer: false)));
    }

    [Fact]
    public void ScrollsImage_ScrollingOff_IsFalse()
    {
        SetDefaults();
        Settings.Zoom.ScrollEnabled = false;

        Assert.False(ContextualActionHandler.ScrollsImage(new FakeScrollTarget(isImageTallerThanViewer: true)));
    }

    [Fact]
    public void ScrollsImage_NoTarget_IsFalse()
    {
        SetDefaults();
        Settings.Zoom.ScrollEnabled = true;

        Assert.False(ContextualActionHandler.ScrollsImage(null));
    }
}
