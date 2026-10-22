using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class InputModeTests
{
    [Fact]
    public void Resolve_NothingActive_IsNormal()
    {
        Assert.Equal(InputMode.Normal, new InputModeState().Resolve());
    }

    [Theory]
    [InlineData(true, false, false, false, false, InputMode.Dialog)]
    [InlineData(false, true, false, false, false, InputMode.Renaming)]
    [InlineData(false, false, true, false, false, InputMode.Crop)]
    [InlineData(false, false, false, true, false, InputMode.MotionPhoto)]
    [InlineData(false, false, false, false, true, InputMode.Slideshow)]
    public void Resolve_OneActive_IsThatMode(
        bool isDialogOpen, bool isRenaming, bool isCropping, bool isMotionPhotoActive, bool isSlideshowRunning,
        InputMode expected)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping, isMotionPhotoActive, isSlideshowRunning);

        Assert.Equal(expected, state.Resolve());
    }

    [Theory]
    // A dialog opened while cropping takes over, so Esc closes the dialog, not the crop
    [InlineData(true, false, true, false, false, InputMode.Dialog)]
    [InlineData(true, true, true, true, true, InputMode.Dialog)]
    [InlineData(false, true, true, false, false, InputMode.Renaming)]
    [InlineData(false, false, true, true, true, InputMode.Crop)]
    // A slideshow reaching a motion photo: the motion photo ranks higher
    [InlineData(false, false, false, true, true, InputMode.MotionPhoto)]
    public void Resolve_SeveralActive_HighestRankedWins(
        bool isDialogOpen, bool isRenaming, bool isCropping, bool isMotionPhotoActive, bool isSlideshowRunning,
        InputMode expected)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping, isMotionPhotoActive, isSlideshowRunning);

        Assert.Equal(expected, state.Resolve());
    }

    [Theory]
    [InlineData(InputMode.Dialog, true)]
    [InlineData(InputMode.Renaming, true)]
    [InlineData(InputMode.Crop, true)]
    [InlineData(InputMode.MotionPhoto, false)]
    [InlineData(InputMode.Slideshow, false)]
    [InlineData(InputMode.Normal, false)]
    public void IsBlocking_OnlyDialogRenamingAndCrop(InputMode mode, bool expected)
    {
        Assert.Equal(expected, mode.IsBlocking());
    }

    [Theory]
    [InlineData(false, false, InputMode.AnimatedImage)]
    // A slideshow reaching an animated image: the animated image ranks higher
    [InlineData(true, false, InputMode.AnimatedImage)]
    [InlineData(true, true, InputMode.Crop)]
    public void Resolve_AnimatedImage_RanksAboveSlideshowAndBelowBlockingModes(
        bool isSlideshowRunning, bool isCropping, InputMode expected)
    {
        var state = new InputModeState(IsCropping: isCropping, IsSlideshowRunning: isSlideshowRunning,
            IsAnimatedImageActive: true);

        Assert.Equal(expected, state.Resolve());
    }

    [Fact]
    public void AnimatedImage_IsNonBlockingAndDisallowsStillImageActions()
    {
        Assert.False(InputMode.AnimatedImage.IsBlocking());
        Assert.True(InputMode.AnimatedImage.DisallowsStillImageActions());
    }
}
