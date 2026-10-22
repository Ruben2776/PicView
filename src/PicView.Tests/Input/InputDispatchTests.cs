using Avalonia.Input;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class InputDispatchTests
{
    private static readonly Keybind Enter = new(Key.Enter);
    private static readonly Keybind Escape = new(Key.Escape);
    private static readonly Keybind Right = new(Key.Right);

    // Enter is both Confirm and ToggleFullscreen; Escape is the fixed Cancel and also bound to Close
    private static readonly Keymap Keymap = Keymap.FromJson(null, """
        {
          "Right": "Next",
          "Enter": "ToggleFullscreen",
          "Escape": "Close",
          "Contextual": { "Enter": "Confirm" }
        }
        """);

    private static readonly InputModeState Normal = new();
    private static readonly InputModeState Crop = new(IsCropping: true);
    private static readonly InputModeState Dialog = new(IsDialogOpen: true);
    private static readonly InputModeState MotionPhotoInSlideshow = new(IsMotionPhotoActive: true, IsSlideshowRunning: true);

    private sealed class FakeHandler(params (InputMode Mode, string Action)[] claims) : IContextualActionHandler
    {
        public List<(InputMode Mode, string Action)> Asked { get; } = [];

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction)
        {
            Asked.Add((mode, contextualAction));
            return ValueTask.FromResult(claims.Contains((mode, contextualAction)));
        }
    }

    private static async Task<(FakeHandler Handler, KeymapResolution Ordinary)> Dispatch(
        Keybind keybind, InputModeState state, params (InputMode, string)[] claims)
    {
        var handler = new FakeHandler(claims);
        var ordinary = await InputDispatch.DispatchAsync(keybind, state, Keymap, handler);
        return (handler, ordinary);
    }

    [Fact]
    public async Task ClaimedContextualAction_RunsInsteadOfOrdinaryAction()
    {
        var (handler, ordinary) = await Dispatch(Enter, Crop, (InputMode.Crop, "Confirm"));

        Assert.Equal([(InputMode.Crop, "Confirm")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task UnclaimedContextualAction_InNormal_FallsThroughToOrdinaryAction()
    {
        var (_, ordinary) = await Dispatch(Enter, Normal);

        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"), ordinary);
    }

    [Fact]
    public async Task UnclaimedContextualAction_InBlockingMode_IsBlocked()
    {
        var (handler, ordinary) = await Dispatch(Escape, Crop);

        Assert.Equal([(InputMode.Crop, "Cancel")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task BlockingMode_OnlyAsksItself()
    {
        var state = new InputModeState(IsDialogOpen: true, IsCropping: true, IsSlideshowRunning: true);

        var (handler, _) = await Dispatch(Escape, state);

        Assert.Equal([(InputMode.Dialog, "Cancel")], handler.Asked);
    }

    [Fact]
    public async Task NonBlockingModes_AskEachActiveModeByRankThenNormal()
    {
        var (handler, ordinary) = await Dispatch(Escape, MotionPhotoInSlideshow);

        Assert.Equal(
            [(InputMode.MotionPhoto, "Cancel"), (InputMode.Slideshow, "Cancel"), (InputMode.Normal, "Cancel")],
            handler.Asked);
        Assert.Equal(KeymapResolution.Action("Close"), ordinary);
    }

    [Fact]
    public async Task NonBlockingModes_StopAtTheFirstModeThatClaims()
    {
        // A stopped motion photo leaves Cancel to the slideshow
        var (handler, ordinary) = await Dispatch(Escape, MotionPhotoInSlideshow, (InputMode.Slideshow, "Cancel"));

        Assert.Equal([(InputMode.MotionPhoto, "Cancel"), (InputMode.Slideshow, "Cancel")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task OrdinaryKeybind_InNonBlockingMode_Runs()
    {
        var (handler, ordinary) = await Dispatch(Right, MotionPhotoInSlideshow);

        Assert.Empty(handler.Asked);
        Assert.Equal(KeymapResolution.Action("Next"), ordinary);
    }

    [Fact]
    public async Task OrdinaryKeybind_InBlockingMode_IsBlocked()
    {
        var (_, ordinary) = await Dispatch(Right, Dialog);

        Assert.Equal(KeymapResolution.None, ordinary);
    }

    private static readonly Keymap CopyKeymap = Keymap.FromJson(null,
        $$"""{ "{{InputDispatch.CopyKeybind.GetFormattedForJSON()}}": "CopyImage" }""");

    [Fact]
    public async Task CopyChord_InCrop_IsTheFixedCopyAction()
    {
        var handler = new FakeHandler((InputMode.Crop, ContextualActionHandler.Copy));

        var ordinary = await InputDispatch.DispatchAsync(InputDispatch.CopyKeybind, Crop, CopyKeymap, handler);

        Assert.Equal([(InputMode.Crop, ContextualActionHandler.Copy)], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task CopyChord_InNormal_FallsThroughToItsOrdinaryAction()
    {
        var handler = new FakeHandler();

        var ordinary = await InputDispatch.DispatchAsync(InputDispatch.CopyKeybind, Normal, CopyKeymap, handler);

        Assert.Equal(KeymapResolution.Action("CopyImage"), ordinary);
    }

    [Theory]
    [InlineData(false, false, false, false, false, false, new[] { InputMode.Normal })]
    [InlineData(true, false, true, false, false, false, new[] { InputMode.Dialog })]
    [InlineData(false, false, false, false, true, false, new[] { InputMode.Slideshow, InputMode.Normal })]
    [InlineData(false, false, false, true, true, false,
        new[] { InputMode.MotionPhoto, InputMode.Slideshow, InputMode.Normal })]
    [InlineData(false, false, false, false, true, true,
        new[] { InputMode.AnimatedImage, InputMode.Slideshow, InputMode.Normal })]
    public async Task UnclaimedContextualAction_AsksTheBlockingModeOrEveryActiveNonBlockingModeThenNormal(
        bool isDialogOpen, bool isRenaming, bool isCropping, bool isMotionPhotoActive, bool isSlideshowRunning,
        bool isAnimatedImageActive, InputMode[] expected)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping, isMotionPhotoActive, isSlideshowRunning,
            isAnimatedImageActive);

        var (handler, _) = await Dispatch(Escape, state);

        Assert.Equal(expected, handler.Asked.Select(asked => asked.Mode));
    }
}
