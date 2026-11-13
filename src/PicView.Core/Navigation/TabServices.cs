using PicView.Core.Thumbnailing;
using PicView.Core.IPlatform;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// The services every tab loads and navigates with, composed once at App Start and handed to each tab when it is
/// created, and the settings every tab follows (ADR-0020).
/// </summary>
public sealed record TabServices(
    SharedImageCache Cache,
    Thumbnails Thumbnails,
    IFileWatcherService Watcher,
    IImageModelLoader ImageLoader,
    IPlatformSpecificService Platform,
    GlobalSettingsViewModel GlobalSettings);
