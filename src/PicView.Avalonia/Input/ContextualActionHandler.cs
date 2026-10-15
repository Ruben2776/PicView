using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Gives the Contextual Actions their meaning in each <see cref="InputMode"/> of a window.
/// </summary>
public sealed class ContextualActionHandler(MainWindow window, MainWindowViewModel vm) : IContextualActionHandler
{
    public const string Confirm = "Confirm";
    public const string Cancel = "Cancel";
    public const string PlayPause = "PlayPause";
    public const string Copy = "Copy";

    public async ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction)
    {
        var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
        switch (mode, contextualAction)
        {
            case (InputMode.Dialog, Cancel):
                await CloseTopDialogAsync().ConfigureAwait(false);
                return true;

            case (InputMode.Crop, Confirm):
                await activeTab.CropService.SaveCropAsync().ConfigureAwait(false);
                return true;
            case (InputMode.Crop, Cancel):
                activeTab.CropService.CloseCropControl();
                return true;
            case (InputMode.Crop, Copy):
                await activeTab.CropService.CopyCroppedImageAsync().ConfigureAwait(false);
                return true;

            case (InputMode.MotionPhoto, PlayPause) when activeTab.CurrentView.CurrentValue is ImageViewer imageViewer:
                imageViewer.ToggleMotionPhotoPlayPause();
                return true;
            case (InputMode.MotionPhoto, Cancel) when activeTab.CurrentView.CurrentValue is ImageViewer imageViewer:
                // A stopped motion photo leaves Cancel to the next mode
                return imageViewer.StopMotionPhotoIfPlaying();

            case (InputMode.Slideshow, Cancel):
                Slideshow.StopSlideshow(vm);
                return true;
            case (InputMode.Slideshow, PlayPause):
                // TODO: Pause and resume the slideshow. Claimed so Space does not run its ordinary Action meanwhile.
                return true;

            // TODO: Make the expanded gallery an Input Mode that overrides Next, Prev, Up and Down and gives
            // Confirm this meaning, instead of those Actions checking the gallery themselves.
            case (InputMode.Normal, Confirm) when activeTab.Gallery.IsGalleryExpanded.CurrentValue:
                await GalleryHelper.GalleryClick(vm).ConfigureAwait(false);
                return true;
            case (InputMode.Normal, Cancel):
                await MainKeyboardShortcuts.CloseOnEscapeAsync(vm).ConfigureAwait(false);
                return true;

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

        var animatedPopUp = window.UIHelper.GetMainView?.MainPanel.Children.OfType<AnimatedPopUp>()
            .FirstOrDefault(popUp => popUp.IsVisible);
        if (animatedPopUp is not null)
        {
            await animatedPopUp.AnimatedClosing().ConfigureAwait(false);
            return;
        }

        vm.TopTitlebarViewModel.CloseMenu();
    }
}
