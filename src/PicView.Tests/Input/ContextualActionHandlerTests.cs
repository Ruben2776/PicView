using System.Globalization;
using PicView.Avalonia.Input;
using PicView.Core.Animation;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
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
        public int ScrollLines { get; private set; }
        public void ScrollLine(bool down) => ScrollLines++;
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
    [InlineData(ContextualActionHandler.Save, nameof(ICropService.SaveCropAsAsync))]
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

    /// <summary>Shows an animated image with three frames, as once a decoder has opened the file.</summary>
    private static AnimationPlayback ShowAnimatedImage(TabViewModel tab)
    {
        tab.Display(new ImageModel
            { FileInfo = new FileInfo("a.gif"), Image = new object(), ImageType = ImageType.AnimatedGif });
        var playback = tab.Animation.Primary.Value!;
        var frame = TimeSpan.FromMilliseconds(100);
        playback.Attach(new FrameTimeline([frame, frame, frame]));
        return playback;
    }

    [Fact]
    public async Task AnimatedImage_PlayPause_PausesAndResumes()
    {
        var (handler, _) = CreateHandler(out var tab);
        var playback = ShowAnimatedImage(tab);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.PlayPause));
        Assert.True(playback.IsPaused);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.PlayPause));
        Assert.False(playback.IsPaused);
    }

    [Fact]
    public async Task AnimatedImage_NextAndPrevFrame_PauseAndStepWrapping()
    {
        var (handler, _) = CreateHandler(out var tab);
        var playback = ShowAnimatedImage(tab);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.NextFrame));
        Assert.True(playback.IsPaused);
        Assert.Equal(1, playback.CurrentFrame);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.PrevFrame));
        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.PrevFrame));
        Assert.Equal(2, playback.CurrentFrame);
    }

    [Fact]
    public async Task AnimatedImage_SpeedActions_MoveThePlaybackSpeed()
    {
        var (handler, _) = CreateHandler(out var tab);
        var playback = ShowAnimatedImage(tab);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.SlowDown));
        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.SlowDown));
        Assert.Equal(0.25, playback.Speed);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.SpeedUp));
        Assert.Equal(0.5, playback.Speed);

        Assert.True(await handler.TryRunAsync(InputMode.AnimatedImage, ContextualActionHandler.ResetSpeed));
        Assert.Equal(1, playback.Speed);
        Assert.False(playback.IsPaused);
    }

    [Theory]
    [InlineData(ContextualActionHandler.Up)]
    [InlineData(ContextualActionHandler.Down)]
    [InlineData(ContextualActionHandler.Left)]
    [InlineData(ContextualActionHandler.Right)]
    [InlineData(ContextualActionHandler.Confirm)]
    [InlineData(ContextualActionHandler.Cancel)]
    public async Task AnimatedImage_OtherContextualActions_AreNotClaimed(string action)
    {
        // So the arrows keep rotating and going to the next file on an animated image
        var (handler, _) = CreateHandler(out var tab);
        ShowAnimatedImage(tab);

        Assert.False(await handler.TryRunAsync(InputMode.AnimatedImage, action));
    }

    [Theory]
    [InlineData(ContextualActionHandler.PlayPause)]
    [InlineData(ContextualActionHandler.NextFrame)]
    [InlineData(ContextualActionHandler.SpeedUp)]
    public async Task AnimatedImage_WithNoPlayback_IsNotClaimed(string action)
    {
        // As while a Stand-in shows, so Space falls through to its ordinary Action
        var (handler, _) = CreateHandler(out var tab);
        ShowAnimatedImage(tab);
        tab.ShowStandIn(new object());

        Assert.False(await handler.TryRunAsync(InputMode.AnimatedImage, action));
    }

    [Theory]
    [InlineData(ContextualActionHandler.NextFrame)]
    [InlineData(ContextualActionHandler.SpeedUp)]
    public async Task Normal_AnimationActions_AreNotClaimed(string action)
    {
        var (handler, _) = CreateHandler(out var tab);
        ShowAnimatedImage(tab);

        Assert.False(await handler.TryRunAsync(InputMode.Normal, action));
    }

    [Theory]
    [InlineData(0.25, "0.25×")]
    [InlineData(0.5, "0.5×")]
    [InlineData(1, "1×")]
    [InlineData(4, "4×")]
    public void FormatSpeed_DropsTrailingZeros(double speed, string expected)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.Equal(expected, AnimatedImageActions.FormatSpeed(speed));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void FormatFrame_CountsFromOne()
    {
        Assert.Equal("Frame 1 / 12", AnimatedImageActions.FormatFrame("Frame", 0, 12));
        Assert.Equal("12 / 12", AnimatedImageActions.FormatFrame(null, 11, 12));
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
        // The window's value is passed in; the record is never read
        SetDefaults();
        Settings.Zoom.ScrollEnabled = false;

        Assert.True(ContextualActionHandler.ScrollsImage(true, new FakeScrollTarget(isImageTallerThanViewer: true)));
    }

    [Fact]
    public void ScrollsImage_ScrollingOnAndFits_IsFalse()
    {
        SetDefaults();

        Assert.False(ContextualActionHandler.ScrollsImage(true, new FakeScrollTarget(isImageTallerThanViewer: false)));
    }

    [Fact]
    public void ScrollsImage_ScrollingOff_IsFalse()
    {
        SetDefaults();
        Settings.Zoom.ScrollEnabled = true;

        Assert.False(ContextualActionHandler.ScrollsImage(false, new FakeScrollTarget(isImageTallerThanViewer: true)));
    }

    [Fact]
    public void ScrollsImage_NoTarget_IsFalse()
    {
        SetDefaults();

        Assert.False(ContextualActionHandler.ScrollsImage(true, null));
    }

    [AvaloniaFact]
    public async Task Normal_UpWithScrollingOnInAnotherWindowOnly_IsLeftToItsActionHere_AndScrollsThere()
    {
        // Each window's Up reads its own scrolling, so a toggle in one leaves the other's keys their meaning
        var (here, _) = CreateHandler(out var hereTab);
        var (there, _) = CreateHandler(out var thereTab);
        var hereTarget = new FakeScrollTarget(isImageTallerThanViewer: true);
        var thereTarget = new FakeScrollTarget(isImageTallerThanViewer: true);
        hereTab.SetViews(new FakeViews(hereTarget));
        thereTab.SetViews(new FakeViews(thereTarget));
        thereTab.ParentWindowContext.WindowSettings.IsScrollingEnabled.Value = true;

        Assert.False(await here.TryRunAsync(InputMode.Normal, ContextualActionHandler.Up));
        Assert.True(await there.TryRunAsync(InputMode.Normal, ContextualActionHandler.Up));

        Assert.Equal(0, hereTarget.ScrollLines);
        Assert.Equal(1, thereTarget.ScrollLines);
        Assert.False(hereTab.ParentWindowContext.WindowSettings.IsScrollingEnabled.Value);
    }

    private sealed class FakeViews(object view) : ITabViews
    {
        public IReadOnlyBindableReactiveProperty<object?> View { get; } = new BindableReactiveProperty<object?>(view);

        public void Dispose()
        {
        }
    }
}
