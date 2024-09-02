namespace PicView.Core.Gallery;
    /// <summary>
    /// Used for determining animation to play when switching between gallery modes
    /// </summary>
    public enum GalleryMode
    {
        FullToBottom,
        FullToClosed,
        BottomToFull,
        BottomToClosed,
        ClosedToFull,
        ClosedToBottom,
        Closed,
        BottomNoAnimation
    }
