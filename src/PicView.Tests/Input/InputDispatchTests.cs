using Avalonia.Input;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class InputDispatchTests
{
    private static readonly Keybind Enter = new(Key.Enter);
    private static readonly Keybind Escape = new(Key.Escape);
    private static readonly Keybind Right = new(Key.Right);

    // Enter is both Confirm and ToggleFullscreen; Escape is Cancel and Close
    private static readonly Keymap Keymap = Keymap.FromJson(null, """
        {
          "Right": "Next",
          "Enter": "ToggleFullscreen",
          "Escape": "Close",
          "Contextual": { "Enter": "Confirm", "Escape": "Cancel" }
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

    private static async Task<(FakeHandler Handler, KeymapResolution? Ordinary)> Dispatch(
        Keybind keybind, InputModeState state, params (InputMode, string)[] claims)
    {
        var handler = new FakeHandler(claims);
        KeymapResolution? ordinary = null;
        await InputDispatch.DispatchAsync(keybind, state, Keymap, handler, resolution =>
        {
            ordinary = resolution;
            return ValueTask.CompletedTask;
        });
        return (handler, ordinary);
    }

    [Fact]
    public async Task ClaimedContextualAction_RunsInsteadOfOrdinaryAction()
    {
        var (handler, ordinary) = await Dispatch(Enter, Crop, (InputMode.Crop, "Confirm"));

        Assert.Equal([(InputMode.Crop, "Confirm")], handler.Asked);
        Assert.Null(ordinary);
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
        Assert.Null(ordinary);
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
        Assert.Null(ordinary);
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

        Assert.Null(ordinary);
    }

    [Theory]
    [InlineData(false, false, false, false, false, new[] { InputMode.Normal })]
    [InlineData(true, false, true, false, false, new[] { InputMode.Dialog })]
    [InlineData(false, false, false, false, true, new[] { InputMode.Slideshow, InputMode.Normal })]
    [InlineData(false, false, false, true, true, new[] { InputMode.MotionPhoto, InputMode.Slideshow, InputMode.Normal })]
    public void ContextualChain_IsTheBlockingModeOrEveryActiveNonBlockingModeThenNormal(
        bool isDialogOpen, bool isRenaming, bool isCropping, bool isMotionPhotoActive, bool isSlideshowRunning,
        InputMode[] expected)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping, isMotionPhotoActive, isSlideshowRunning);

        Assert.Equal(expected, state.ContextualChain());
    }
}
