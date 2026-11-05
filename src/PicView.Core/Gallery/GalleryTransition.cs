namespace PicView.Core.Gallery;

/// <summary>
///     How a change between two gallery modes animates. <see cref="AppliesLookAtEnd" /> is false when the new look is
///     applied before the animation starts, or when the change hides the gallery.
/// </summary>
public readonly record struct GalleryTransition(
    double Speed,
    bool TweensBothAxes,
    bool AppliesLookAtEnd,
    bool LocksWrap)
{
    /// <summary>
    ///     The animation for a mode change, or null when the mode does not change.
    /// </summary>
    public static GalleryTransition? For(GalleryMode from, GalleryMode to)
    {
        switch (from, to)
        {
            case (GalleryMode.Closed, GalleryMode.Docked):
                return new GalleryTransition(GalleryDefaults.FastAnimationSpeed, false, false, false);
            case (GalleryMode.Docked, GalleryMode.Closed):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, false, false, false);
            case (GalleryMode.Docked, GalleryMode.Expanded):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, false, false, true);
            case (GalleryMode.Expanded, GalleryMode.Docked):
                return new GalleryTransition(GalleryDefaults.SlowAnimationSpeed, false, true, true);
            case (GalleryMode.Closed, GalleryMode.Expanded):
                return new GalleryTransition(GalleryDefaults.MediumAnimationSpeed, true, false, true);
            case (GalleryMode.Expanded, GalleryMode.Closed):
                return new GalleryTransition(GalleryDefaults.FastAnimationSpeed, true, false, true);
            default:
                return null;
        }
    }
}
