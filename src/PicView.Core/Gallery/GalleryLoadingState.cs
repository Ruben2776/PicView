namespace PicView.Core.Gallery;

public enum GalleryLoadingState
{
    /// <summary>
    /// No items yet.
    /// </summary>
    NotLoaded,

    /// <summary>
    /// A load is reading the files around the current image, and no items are added yet.
    /// </summary>
    Starting,

    /// <summary>
    /// Every item is in, and sizes are still being read.
    /// </summary>
    Loading,

    /// <summary>
    /// Every item's details have been read.
    /// </summary>
    Loaded
}
