using System.Runtime.InteropServices;
using PicView.Core.Thumbnailing;

namespace PicView.Core.Gallery;

/// <summary>
/// The part of a gallery's look that belongs to its Main Window: the Thumbnail Stretch and item size of the Docked
/// and the Expanded Gallery, the Gallery Display Mode and the edge the Docked Gallery sits at. A tab's gallery derives
/// its Gallery Appearance from the look of the window it is in, so a tab that enters a window that differs in several
/// of these takes them all in one change.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct GalleryLook(
    double DockedItemSize,
    double ExpandedItemSize,
    GalleryStretchMode DockedStretch,
    GalleryStretchMode ExpandedStretch,
    GalleryDisplayMode ExpandedDisplayMode,
    GalleryDockPosition Dock)
{
    /// <summary>
    /// The height the gallery's thumbnails are asked for at with this look.
    /// </summary>
    public uint ThumbnailHeight => Thumbnails.GalleryHeight(DockedItemSize, ExpandedItemSize);
}
