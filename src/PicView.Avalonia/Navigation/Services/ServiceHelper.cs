using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation.Services;

public static class ServiceHelper
{
    public static AvaloniaImageModelLoader? ImageLoader { get; private set; }
    public static void SetAvaloniaImageLoader(CoreViewModel core)
    {
        ImageLoader = new AvaloniaImageModelLoader(core);
    }

    public static AvaloniaThumbnailLoader? ThumbLoader { get; private set; }
    public static void SetGalleryLoader()
    {
        ThumbLoader = new AvaloniaThumbnailLoader();
    }
}