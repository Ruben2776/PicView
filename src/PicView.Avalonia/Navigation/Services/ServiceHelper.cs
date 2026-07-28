using PicView.Core.ArchiveHandling;

namespace PicView.Avalonia.Navigation.Services;

public static class ServiceHelper
{
    public static AvaloniaImageLoader? ImageLoader { get; private set; }
    public static void SetAvaloniaImageLoader()
    {
        ImageLoader = new AvaloniaImageLoader();
    }

    public static AvaloniaThumbnailLoader? ThumbLoader { get; private set; }
    public static void SetGalleryLoader()
    {
        ThumbLoader = new AvaloniaThumbnailLoader();
    }

    /// <summary>
    ///     Creates a new <see cref="ArchiveExtractionService"/> instance.
    ///     Each tab should own its own service for isolated archive extraction state.
    /// </summary>
    public static ArchiveExtractionService CreateArchiveExtractionService()
    {
        return new ArchiveExtractionService();
    }
}