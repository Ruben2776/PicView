using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Decides what the Up and Down actions do. With the gallery expanded they move through it, and otherwise they rotate
/// the image.
/// </summary>
public static class ArrowKeyNavigation
{
    /// <param name="isGalleryExpanded">Whether the active tab's gallery is expanded.</param>
    /// <param name="navigate">Moves to the file in the given direction.</param>
    /// <param name="rotateRight">Rotates the image to the right.</param>
    public static async ValueTask Up(
        Func<bool> isGalleryExpanded,
        Func<NavigateTo, ValueTask> navigate,
        Func<ValueTask> rotateRight)
    {
        if (isGalleryExpanded())
        {
            await navigate(NavigateTo.Up).ConfigureAwait(false);
            return;
        }

        await rotateRight().ConfigureAwait(false);
    }

    /// <param name="isGalleryExpanded">Whether the active tab's gallery is expanded.</param>
    /// <param name="navigate">Moves to the file in the given direction.</param>
    /// <param name="rotateLeft">Rotates the image to the left.</param>
    public static async ValueTask Down(
        Func<bool> isGalleryExpanded,
        Func<NavigateTo, ValueTask> navigate,
        Func<ValueTask> rotateLeft)
    {
        if (isGalleryExpanded())
        {
            await navigate(NavigateTo.Down).ConfigureAwait(false);
            return;
        }

        await rotateLeft().ConfigureAwait(false);
    }
}
