using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What an Up or Down press does.
/// </summary>
public enum ArrowKeyAction
{
    Navigate,
    Scroll,
    Rotate
}

/// <summary>
/// Decides what the Up and Down actions do. With the gallery expanded they move through it, while scrolling is on and
/// the image is taller than the image viewer they scroll it, and otherwise they rotate the image.
/// </summary>
public static class ArrowKeyNavigation
{
    /// <param name="isGalleryExpanded">Whether the active tab's gallery is expanded.</param>
    /// <param name="canScroll">Whether the shown image is taller than the image viewer.</param>
    public static ArrowKeyAction Decide(bool isGalleryExpanded, bool canScroll)
    {
        if (isGalleryExpanded)
        {
            return ArrowKeyAction.Navigate;
        }

        return Settings.Zoom.ScrollEnabled && canScroll ? ArrowKeyAction.Scroll : ArrowKeyAction.Rotate;
    }

    /// <summary>
    /// Runs Up for the window's active tab. Reads its inputs straight from the view model, so a press allocates nothing.
    /// </summary>
    public static ValueTask Up(MainWindowViewModel vm, MainWindow mainWindow)
    {
        switch (Decide(IsGalleryExpanded(vm), CanScrollImage(vm)))
        {
            case ArrowKeyAction.Navigate:
                return vm.WindowTabs.NavigateDirectionalAsync(mainWindow.KeybindRouter.IsKeyHeldDown, NavigateTo.Up);
            case ArrowKeyAction.Scroll:
                return ViewerActions.ScrollLine(vm, down: false);
            default:
                RotationManager.RotateRight(vm, mainWindow);
                return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Runs Down for the window's active tab. Reads its inputs straight from the view model, so a press allocates nothing.
    /// </summary>
    public static ValueTask Down(MainWindowViewModel vm, MainWindow mainWindow)
    {
        switch (Decide(IsGalleryExpanded(vm), CanScrollImage(vm)))
        {
            case ArrowKeyAction.Navigate:
                return vm.WindowTabs.NavigateDirectionalAsync(mainWindow.KeybindRouter.IsKeyHeldDown, NavigateTo.Down);
            case ArrowKeyAction.Scroll:
                return ViewerActions.ScrollLine(vm, down: true);
            default:
                RotationManager.RotateLeft(vm, mainWindow);
                return ValueTask.CompletedTask;
        }
    }

    /// <param name="isGalleryExpanded">Whether the active tab's gallery is expanded.</param>
    /// <param name="navigate">Moves to the file in the given direction.</param>
    /// <param name="canScroll">Whether the shown image is taller than the image viewer.</param>
    /// <param name="scrollUp">Scrolls the image viewer up one line.</param>
    /// <param name="rotateRight">Rotates the image to the right.</param>
    public static async ValueTask Up(
        Func<bool> isGalleryExpanded,
        Func<NavigateTo, ValueTask> navigate,
        Func<bool> canScroll,
        Func<ValueTask> scrollUp,
        Func<ValueTask> rotateRight)
    {
        var expanded = isGalleryExpanded();
        switch (Decide(expanded, !expanded && canScroll()))
        {
            case ArrowKeyAction.Navigate:
                await navigate(NavigateTo.Up).ConfigureAwait(false);
                break;
            case ArrowKeyAction.Scroll:
                await scrollUp().ConfigureAwait(false);
                break;
            default:
                await rotateRight().ConfigureAwait(false);
                break;
        }
    }

    /// <param name="isGalleryExpanded">Whether the active tab's gallery is expanded.</param>
    /// <param name="navigate">Moves to the file in the given direction.</param>
    /// <param name="canScroll">Whether the shown image is taller than the image viewer.</param>
    /// <param name="scrollDown">Scrolls the image viewer down one line.</param>
    /// <param name="rotateLeft">Rotates the image to the left.</param>
    public static async ValueTask Down(
        Func<bool> isGalleryExpanded,
        Func<NavigateTo, ValueTask> navigate,
        Func<bool> canScroll,
        Func<ValueTask> scrollDown,
        Func<ValueTask> rotateLeft)
    {
        var expanded = isGalleryExpanded();
        switch (Decide(expanded, !expanded && canScroll()))
        {
            case ArrowKeyAction.Navigate:
                await navigate(NavigateTo.Down).ConfigureAwait(false);
                break;
            case ArrowKeyAction.Scroll:
                await scrollDown().ConfigureAwait(false);
                break;
            default:
                await rotateLeft().ConfigureAwait(false);
                break;
        }
    }

    private static bool IsGalleryExpanded(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.Value.Gallery.IsGalleryExpanded.Value;

    private static bool CanScrollImage(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.Value.CurrentView.CurrentValue is ImageViewer { IsImageTallerThanViewer: true };
}
