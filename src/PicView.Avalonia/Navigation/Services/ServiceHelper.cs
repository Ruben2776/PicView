using PicView.Core.ArchiveHandling;

namespace PicView.Avalonia.Navigation.Services;

public static class ServiceHelper
{
    public static AvaloniaImageModelLoader? ImageLoader { get; private set; }
    public static void SetAvaloniaImageLoader()
    {
        ImageLoader = new AvaloniaImageModelLoader();
    }

    public static AvaloniaThumbnailLoader? ThumbLoader { get; private set; }
    public static void SetGalleryLoader()
    {
        ThumbLoader = new AvaloniaThumbnailLoader();
    }
}