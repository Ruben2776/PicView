namespace PicView.Core.Gallery;

/// <summary>
/// How the Expanded Gallery arranges its Gallery Items. Every mode scrolls down. The Docked Gallery has no display
/// mode. The settings file stores the numbers, so a value keeps its number and a removed number is never reused.
/// </summary>
public enum GalleryDisplayMode
{
    /// <summary>Fills rows of cells of one size, so the thumbnails line up in even columns.</summary>
    Grid = 0,

    /// <summary>Fills equal-width columns, each thumbnail in its own proportions.</summary>
    Masonry = 1,

    /// <summary>Fills rows of equal height across the whole width.</summary>
    Rows = 2
}

public static class GalleryDisplayModes
{
    /// <summary>
    /// The mode the Expanded Gallery's switch button goes to: from Grid to the last used tile mode,
    /// from anything else back to Grid.
    /// </summary>
    public static GalleryDisplayMode Toggle(GalleryDisplayMode current, bool rowsWasLastUsed)
    {
        if (current == GalleryDisplayMode.Grid)
        {
            return rowsWasLastUsed ? GalleryDisplayMode.Rows : GalleryDisplayMode.Masonry;
        }

        return GalleryDisplayMode.Grid;
    }

    /// <summary>Whether the mode shows Gallery Items as tiles, which is Masonry and Rows.</summary>
    public static bool ShowsTiles(GalleryDisplayMode mode)
    {
        return mode is GalleryDisplayMode.Masonry or GalleryDisplayMode.Rows;
    }

    /// <summary>The stored mode itself when it is one of the three, else Grid.</summary>
    public static GalleryDisplayMode Validate(GalleryDisplayMode stored)
    {
        return stored is GalleryDisplayMode.Grid or GalleryDisplayMode.Masonry or GalleryDisplayMode.Rows
            ? stored
            : GalleryDisplayMode.Grid;
    }
}
