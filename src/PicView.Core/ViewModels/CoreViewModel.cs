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

    // Set once by InitializeNavigation, for every Main Window
    public INavigationService? SharedNavigationService { get; private set; }
    public IThumbnailLoader? SharedThumbnailLoader { get; private set; }
    public IFileWatcherService? SharedFileWatcher { get; private set; }

    /// <summary>
    /// Sets up navigation for every Main Window, once, when the first tab opens a source: after the first window and
    /// image are shown, so they show sooner. Later calls do nothing.
    /// </summary>
    public void InitializeNavigation(INavigationService navigation, IThumbnailLoader thumbnailLoader,
        IFileWatcherService fileWatcher)
    {
        if (SharedNavigationService is not null)
        {
            return;
        }

        SharedThumbnailLoader = thumbnailLoader;
        SharedFileWatcher = fileWatcher;
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