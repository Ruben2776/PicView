using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;

namespace PicView.Core.ViewModels;

public class CoreViewModel(
    IPlatformSpecificService? platformSpecificService,
    Func<FileInfo, ValueTask<ImageModel>> imageLoader)
{
    // Shared Services
    public IPlatformSpecificService? PlatformService { get; } = platformSpecificService;
    public SharedImageCache SharedCache { get; } = new(imageLoader);
    public ThumbnailCache SharedThumbnailCache { get; } = new();

    /// <summary>
    /// The services every tab of every Main Window is created with, set once. Null until
    /// <see cref="InitializeNavigation"/> has run.
    /// </summary>
    public TabServices? Services { get; private set; }
    public INavigationService? SharedNavigationService { get; private set; }
    public IThumbnailLoader? SharedThumbnailLoader => Services?.ThumbnailLoader;
    public IFileWatcherService? SharedFileWatcher => Services?.Watcher;

    /// <summary>
    /// Keeps the services every Main Window and tab shares, composed once at App Start. Later calls do nothing.
    /// </summary>
    public void InitializeNavigation(TabServices services, INavigationService navigation)
    {
        if (Services is not null)
        {
            return;
        }

        Services = services;
        SharedNavigationService = navigation;
    }

    // --- Globally Shared State ---
    public TranslationViewModel Translation { get; } = new();
    public GlobalSettingsViewModel? GlobalSettings { get; } = new();
    public GallerySharedSettingsViewModel GallerySettings { get; } = new();
    public KeybindingsViewModel? Keybindings { get; set; }
    public SettingsViewModel? SettingsViewModel { get; set; } // Single settings window
    public AboutViewModel? AboutView { get; set; } // Single about window
    public EffectsViewModel? Effects { get; set; }
    public BatchResizeViewModel? BatchResize { get; set; }
    public FileAssociationsViewModel? AssociationsViewModel { get; set; }
    
    // --- Overview models ---
    public MainWindowOverviewViewModel MainWindows { get; } = new();
}