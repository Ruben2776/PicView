using Avalonia.Input;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class InputDispatchTests
{
    private static readonly Keybind Enter = new(Key.Enter);
    private static readonly Keybind Escape = new(Key.Escape);
    private static readonly Keybind Right = new(Key.Right);
    private static readonly Keybind Up = new(Key.Up);
    private static readonly Keybind Space = new(Key.Space);
    private static readonly Keybind C = new(Key.C);

    // Enter is both Confirm and ToggleFullscreen; Escape is the fixed Cancel and also bound to Close
    private static readonly Keymap Keymap = Keymap.FromJson(null, """
        {
          "Right": "Next",
          "Enter": "ToggleFullscreen",
          "Escape": "Close",
          "Up": "RotateRight",
          "C": "Crop",
          "Contextual": { "Enter": "Confirm", "Up": "Up", "Space": "PlayPause" }
        }
        """);

    private static readonly InputModeState Normal = new();
    private static readonly InputModeState Crop = new(IsCropping: true);
    private static readonly InputModeState Dialog = new(IsDialogOpen: true);
    private static readonly InputModeState Gallery = new(IsGalleryExpanded: true);

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
        var state = new InputModeState(IsDialogOpen: true, IsCropping: true);

        var (handler, _) = await Dispatch(Escape, state);

        Assert.Equal([(InputMode.Dialog, "Cancel")], handler.Asked);
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

    private static readonly Keymap SaveKeymap = Keymap.FromJson(null,
        $$"""{ "{{InputDispatch.SaveKeybind.GetFormattedForJSON()}}": "Save" }""");

    [Fact]
    public async Task SaveChord_InCrop_IsTheFixedSaveAction()
    {
        var handler = new FakeHandler((InputMode.Crop, ContextualActionHandler.Save));

        var ordinary = await InputDispatch.DispatchAsync(InputDispatch.SaveKeybind, Crop, SaveKeymap, handler);

        Assert.Equal([(InputMode.Crop, ContextualActionHandler.Save)], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task SaveChord_InNormal_FallsThroughToItsOrdinaryAction()
    {
        var handler = new FakeHandler();

        var ordinary = await InputDispatch.DispatchAsync(InputDispatch.SaveKeybind, Normal, SaveKeymap, handler);

        Assert.Equal(KeymapResolution.Action("Save"), ordinary);
    }

    [Theory]
    [InlineData(false, false, false, false, false, new[] { InputMode.Normal })]
    [InlineData(true, false, true, false, false, new[] { InputMode.Dialog })]
    [InlineData(false, false, false, true, false,
        new[] { InputMode.MotionPhoto, InputMode.Normal })]
    [InlineData(false, false, false, false, true,
        new[] { InputMode.AnimatedImage, InputMode.Normal })]
    public async Task UnclaimedContextualAction_AsksTheBlockingModeOrEveryActiveNonBlockingModeThenNormal(
        bool isDialogOpen, bool isRenaming, bool isCropping, bool isMotionPhotoActive,
        bool isAnimatedImageActive, InputMode[] expected)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping, isMotionPhotoActive,
            isAnimatedImageActive);

        var (handler, _) = await Dispatch(Escape, state);

        Assert.Equal(expected, handler.Asked.Select(asked => asked.Mode));
    }

    [Fact]
    public async Task Dispatch_GalleryClaimsConfirm_ReturnsNone()
    {
        var (handler, ordinary) = await Dispatch(Enter, Gallery, (InputMode.Gallery, "Confirm"));

        Assert.Equal([(InputMode.Gallery, "Confirm")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task Dispatch_GalleryClaimsUp_ReturnsNone()
    {
        var (handler, ordinary) = await Dispatch(Up, Gallery, (InputMode.Gallery, "Up"));

        Assert.Equal([(InputMode.Gallery, "Up")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task Dispatch_UpUnclaimedInNormal_FallsThroughToRotateRight()
    {
        var (_, ordinary) = await Dispatch(Up, Normal);

        Assert.Equal(KeymapResolution.Action("RotateRight"), ordinary);
    }

    [Fact]
    public async Task Dispatch_GalleryOverMotionPhoto_AsksGalleryThenMotionPhoto()
    {
        var state = new InputModeState(IsMotionPhotoActive: true, IsGalleryExpanded: true);

        var (handler, ordinary) = await Dispatch(Space, state, (InputMode.MotionPhoto, "PlayPause"));

        Assert.Equal([(InputMode.Gallery, "PlayPause"), (InputMode.MotionPhoto, "PlayPause")], handler.Asked);
        Assert.Equal(KeymapResolution.None, ordinary);
    }

    [Fact]
    public async Task Dispatch_CropActionInGallery_IsNotResolved()
    {
        var (_, ordinary) = await Dispatch(C, Gallery);

        Assert.Equal(KeymapResolution.None, ordinary);
    }
}
