namespace PicView.Core.Gallery;

/// <summary>
///     What a gallery takes from the window around it.
/// </summary>
public static class GalleryDocking
{
    /// <summary>
    ///     The room a Docked Gallery takes from the image: its thickness as the width for a Left or Right dock and as
    ///     the height for a Top or Bottom one. Nothing for a gallery that is not docked or not shown.
    /// </summary>
    public static (double Width, double Height) ReservedBy(GalleryAppearance appearance)
    {
        if (appearance is not { Mode: GalleryMode.Docked, IsShown: true })
        {
            return (0, 0);
        }

        return appearance.Dock switch
        {
            GalleryDockPosition.Left or GalleryDockPosition.Right => (appearance.Extent, 0),
            GalleryDockPosition.Top or GalleryDockPosition.Bottom => (0, appearance.Extent),
            _ => (0, 0)
        };
    }
}
