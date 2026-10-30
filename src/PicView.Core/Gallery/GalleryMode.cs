namespace PicView.Core.Gallery;

/// <summary>
/// The state a tab's gallery is in, exactly one at a time. Whether a change animates travels with the change
/// (<see cref="GalleryModeChange"/>), not in the mode.
/// </summary>
public enum GalleryMode
{
    Closed,
    Docked,
    Expanded
}
