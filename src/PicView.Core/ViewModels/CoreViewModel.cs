using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Thumbnailing;

namespace PicView.Core.ViewModels;

public class CoreViewModel
{
    /// <param name="platformSpecificService">The platform's services.</param>
    /// <param name="imageLoader">
    /// Loads an <see cref="ImageModel"/> for the <see cref="SharedCache"/>, given this view model, so decoders that
    /// need it (such as the shell decoder) don't have to look it up.
    /// </param>
    /// <param name="globalSettings">The settings every tab follows; a test hands in one with a fake save.</param>
    /// <param name="saveGallerySettings">Saves the gallery settings; a test hands in one that writes no file.</param>
    /// <param name="timeProvider">The clock slideshows tick on; tests give their own.</param>
    public CoreViewModel(
        IPlatformSpecificService? platformSpecificService,
        Func<FileInfo, CoreViewModel, ImageModel> imageLoader,
        GlobalSettingsViewModel? globalSettings = null,
        Func<ValueTask<bool>>? saveGallerySettings = null,
        TimeProvider? timeProvider = null)
    {
        PlatformService = platformSpecificService;
        // One closure for the life of the app, not one per load.
        SharedCache = new SharedImageCache(file => imageLoader(file, this));
        Time = timeProvider ?? TimeProvider.System;
        Screensaver = new ScreensaverHold(platformSpecificService);
        GlobalSettings = globalSettings ?? new GlobalSettingsViewModel();
        GallerySettings = new GallerySharedSettingsViewModel(saveGallerySettings);
    }

    /// <summary>For a loader that does not need the view model, as most tests have.</summary>
    public CoreViewModel(
        IPlatformSpecificService? platformSpecificService,
        Func<FileInfo, ImageModel> imageLoader,
        GlobalSettingsViewModel? globalSettings = null,
        Func<ValueTask<bool>>? saveGallerySettings = null,
        TimeProvider? timeProvider = null)
        : this(platformSpecificService, (file, _) => imageLoader(file), globalSettings, saveGallerySettings,
            timeProvider)
    {
    }

    // Shared Services
    public IPlatformSpecificService? PlatformService { get; }
    public SharedImageCache SharedCache { get; }

    /// <summary>
    /// The clock slideshows tick on; tests give their own.
    /// </summary>
    public TimeProvider Time { get; }

    /// <summary>
    /// Keeps the screensaver off while any Main Window runs a slideshow.
    /// </summary>
    public ScreensaverHold Screensaver { get; }

    /// <summary>
    /// The services every tab of every Main Window is created with, set once. Null until
    /// <see cref="InitializeNavigation"/> has run.
    /// </summary>
    public TabServices? Services { get; private set; }
    public INavigationService? SharedNavigationService { get; private set; }
    public Thumbnails? SharedThumbnails => Services?.Thumbnails;
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
    /// <summary>The settings every tab follows; a test hands in one with a fake save.</summary>
    public GlobalSettingsViewModel GlobalSettings { get; }
    /// <summary>The gallery settings every tab follows; a test hands in a save that writes no file.</summary>
    public GallerySharedSettingsViewModel GallerySettings { get; }
    public KeybindingsViewModel? Keybindings { get; set; }
    public SettingsViewModel? SettingsViewModel { get; set; } // Single settings window
    public AboutViewModel? AboutView { get; set; } // Single about window
    public EffectsViewModel? Effects { get; set; }
    public BatchResizeViewModel? BatchResize { get; set; }
    public FileAssociationsViewModel? AssociationsViewModel { get; set; }
    
    // --- Overview models ---
    public MainWindowOverviewViewModel MainWindows { get; } = new();
}