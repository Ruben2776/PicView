using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Gives the Contextual Actions their meaning in each <see cref="InputMode"/> of a window.
/// </summary>
public sealed class ContextualActionHandler(MainWindow window, MainWindowViewModel vm) : IContextualActionHandler
{
    public const string Confirm = "Confirm";

    /// <summary>
    /// Not a bindable Contextual Action: dispatch gives it to the fixed <see cref="InputDispatch.CancelKeybind"/>.
    /// </summary>
    public const string Cancel = "Cancel";
    public const string PlayPause = "PlayPause";

    /// <summary>
    /// Not a bindable Contextual Action: dispatch gives it to the fixed <see cref="InputDispatch.CopyKeybind"/>.
    /// </summary>
    public const string Copy = "Copy";

    // Move through the expanded gallery; Up and Down also scroll an image taller than the viewer
    public const string Up = "Up";
    public const string Down = "Down";
    public const string Left = "Left";
    public const string Right = "Right";

    /// <summary>
    /// Whether Up and Down scroll <paramref name="target"/>: scrolling is on and the image is taller than the viewer.
    /// Otherwise they fall through to rotating the image.
    /// </summary>
    internal static bool ScrollsImage(IZoomScrollTarget? target) =>
        Settings.Zoom.ScrollEnabled && target is { IsImageTallerThanViewer: true };

    public async ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction)
    {
        var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
        switch (mode, contextualAction)
        {
            case (InputMode.Dialog, Confirm):
                return TopPopUp(window.UIHelper.GetMainView?.MainPanel) is { } popUp &&
                       await popUp.ConfirmAsync().ConfigureAwait(false);
            case (InputMode.Dialog, Cancel):
                await CloseTopDialogAsync().ConfigureAwait(false);
                return true;

            case (InputMode.Crop, Confirm):
                await activeTab.CropService.SaveCropAsAsync().ConfigureAwait(false);
                return true;
            case (InputMode.Crop, Cancel):
                activeTab.CropService.CloseCropControl();
                return true;
            case (InputMode.Crop, Copy):
                await activeTab.CropService.CopyCroppedImageAsync().ConfigureAwait(false);
                return true;

            case (InputMode.MotionPhoto, PlayPause) when activeTab.CurrentView.Value is ImageViewer imageViewer:
                imageViewer.ToggleMotionPhotoPlayPause();
                return true;
            case (InputMode.MotionPhoto, Cancel) when activeTab.CurrentView.Value is ImageViewer imageViewer:
                // A stopped motion photo leaves Cancel to the next mode
                return imageViewer.StopMotionPhotoIfPlaying();

            // TODO: Give the animated image keybinds their meanings here, such as PlayPause and frame stepping.
            // Until then an animated image claims nothing, so its Keybinds fall through as usual.

            case (InputMode.Slideshow, Cancel):
                Slideshow.StopSlideshow(vm);
                return true;
            case (InputMode.Slideshow, PlayPause):
                // TODO: Pause and resume the slideshow. Claimed so Space does not run its ordinary Action meanwhile.
                return true;

            case (InputMode.Gallery, Up):
                activeTab.Gallery.Navigate(NavigateTo.Up);
                return true;
            case (InputMode.Gallery, Down):
                activeTab.Gallery.Navigate(NavigateTo.Down);
                return true;
            case (InputMode.Gallery, Left):
                activeTab.Gallery.Navigate(NavigateTo.Previous);
                return true;
            case (InputMode.Gallery, Right):
                activeTab.Gallery.Navigate(NavigateTo.Next);
                return true;
            case (InputMode.Gallery, Confirm):
                await GalleryHelper.GalleryClick(vm).ConfigureAwait(false);
                return true;
            case (InputMode.Gallery, Cancel):
                activeTab.Gallery.Switcher.ToggleExpanded();
                return true;

            case (InputMode.Normal, Up) when ScrollsImage(activeTab.CurrentView.Value as IZoomScrollTarget):
                await ViewerActions.ScrollLine(vm, down: false).ConfigureAwait(false);
                return true;
            case (InputMode.Normal, Down) when ScrollsImage(activeTab.CurrentView.Value as IZoomScrollTarget):
                await ViewerActions.ScrollLine(vm, down: true).ConfigureAwait(false);
                return true;
            // Cancel in the Normal mode closes what Escape closes, which the KeybindRouter decides

            default:
                return false;
        }
    }

    /// <summary>
    /// Closes the drop-down menu, else the open pop-up, else the main menu.
    /// </summary>
    private async ValueTask CloseTopDialogAsync()
    {
        if (vm.TopTitlebarViewModel.DropDownMenu?.IsDropDownMenuVisible.CurrentValue ?? false)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
            return;
        }

        var animatedPopUp = TopPopUp(window.UIHelper.GetMainView?.MainPanel);
        if (animatedPopUp is not null)
        {
            await animatedPopUp.AnimatedClosing().ConfigureAwait(false);
            return;
        }

        vm.TopTitlebarViewModel.CloseMenu();
    }

    /// <summary>
    /// Returns the visible pop-up drawn on top in <paramref name="mainPanel"/>, if any.
    /// </summary>
    internal static AnimatedPopUp? TopPopUp(Panel? mainPanel)
    {
        if (mainPanel is null)
        {
            return null;
        }

        // Indexed, since every Keybind asks
        var children = mainPanel.Children;
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is AnimatedPopUp { IsVisible: true } popUp)
            {
                return popUp;
            }
        }

        return null;
    }
}
