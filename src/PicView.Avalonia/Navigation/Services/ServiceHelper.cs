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
}