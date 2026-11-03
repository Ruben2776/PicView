using PicView.Core.IPlatform;
using PicView.Core.Navigation.Interfaces;

namespace PicView.Core.Navigation;

/// <summary>
/// The services every tab loads and navigates with, composed once at App Start and handed to each tab when it is
/// created.
/// </summary>
public sealed record TabServices(
    SharedImageCache Cache,
    IThumbnailCache ThumbnailCache,
    IThumbnailLoader ThumbnailLoader,
    IFileWatcherService Watcher,
    IImageModelLoader ImageLoader,
    IPlatformSpecificService Platform);
