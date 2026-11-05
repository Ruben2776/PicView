namespace PicView.Core.Gallery;

/// <summary>
///     What a gallery takes from the window around it.
/// </summary>
public static class GalleryDocking
{
    /// <summary>
    ///     The room a Docked Gallery takes from the image: its thickness as the width for a Left or Right dock and as
    ///     the height for a Top or Bottom one. An Expanded Gallery that returns to docked keeps the thickness it returns
    ///     to, so a window that fits around it stays still while it expands and closes. Nothing for any other gallery.
    /// </summary>
    /// <param name="returnsToDocked">Whether the gallery is docked, or expanded and gives back docked when it closes.</param>
    public static (double Width, double Height) ReservedBy(GalleryAppearance appearance, bool returnsToDocked = false)
    {
        var extent = appearance switch
        {
            { Mode: GalleryMode.Docked, IsShown: true } => appearance.Extent,
            { Mode: GalleryMode.Expanded, IsShown: true } when returnsToDocked => appearance.DockedExtent,
            _ => 0
        };
        if (extent is 0)
        {
            return (0, 0);
        }

        return appearance.Dock switch
        {
            GalleryDockPosition.Left or GalleryDockPosition.Right => (extent, 0),
            GalleryDockPosition.Top or GalleryDockPosition.Bottom => (0, extent),
            _ => (0, 0)
        };
    }
}
