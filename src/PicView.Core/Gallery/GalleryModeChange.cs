namespace PicView.Core.Gallery;

/// <summary>
/// One change of the gallery mode a tab shows, as <see cref="GalleryModeSwitcher"/> publishes it.
/// </summary>
/// <param name="From">The mode shown before.</param>
/// <param name="To">The mode shown now.</param>
/// <param name="Animate">Whether the view animates the change; starting and hiding never do.</param>
public readonly record struct GalleryModeChange(GalleryMode From, GalleryMode To, bool Animate);
