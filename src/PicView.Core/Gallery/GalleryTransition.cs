namespace PicView.Core.Gallery;

/// <summary>
///     How a change between two gallery modes animates. <see cref="AppliesLookAtEnd" /> is false when the new look is
///     applied before the animation starts, or when the change hides the gallery.
/// </summary>
public readonly record struct GalleryTransition(
    double Speed,
    bool TweensBothAxes,
    bool AppliesLookAtEnd,
    bool LocksWrap,
    bool RefitsAtEnd,
    bool RefitsEveryFrame)
{
    /// <summary>
    ///     The animation for a mode change, or null when the mode does not change.
    /// </summary>
    public static GalleryTransition? For(GalleryMode from, GalleryMode to, GalleryDockPosition dock, bool autoFit)
    {
        switch (from, to)
        {
            case (GalleryMode.Closed, GalleryMode.Docked):
                return new GalleryTransition(GalleryDefaults.FastAnimationSpeed, false, false, false, true, false);
            case (GalleryMode.Docked, GalleryMode.Closed):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, false, false, false, false, false);
            case (GalleryMode.Docked, GalleryMode.Expanded):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, false, false, true, false, false);
            case (GalleryMode.Expanded, GalleryMode.Docked):
                var horizontal = dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;
                return new GalleryTransition(GalleryDefaults.SlowAnimationSpeed, false, true, true, true,
                    horizontal && !autoFit);
            case (GalleryMode.Closed, GalleryMode.Expanded):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, true, false, true, false, false);
            case (GalleryMode.Expanded, GalleryMode.Closed):
                return new GalleryTransition(GalleryDefaults.FastAnimationSpeed, true, false, true, true, false);
            default:
                return null;
        }
    }
}
