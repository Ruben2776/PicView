using System.Diagnostics;
using Avalonia.Controls;
using PicView.Avalonia.Navigation.Services;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

public static class TabNavigationInitializer
{
    /// <inheritdoc cref="Initialize(CoreViewModel, List{FileInfo}, MainWindowViewModel)"/>
    /// <param name="fileInfo">A file whose folder's files the tab browses.</param>
    public static void Initialize(CoreViewModel core, FileInfo fileInfo, MainWindowViewModel vm)
    {
        Debug.Assert(core.PlatformService != null);
        Initialize(core, core.PlatformService.GetFiles(fileInfo), vm);
    }

    /// <summary>
    /// Opens the active tab of <paramref name="vm"/>'s window for the first time, browsing <paramref name="files"/>.
    /// May be called from any thread.
    /// </summary>
    public static void Initialize(CoreViewModel core, List<FileInfo> files, MainWindowViewModel vm)
    {
        // --- Initialization Logic ---
        // This is the initialization logic for the navigation system.
        // It is initialized after initial image load, to make it feel faster by showing the image asap. 
        
        InitializeNavigation(core);

        var tabOverView = vm.WindowTabs;
        var tab = tabOverView.ActiveTab.CurrentValue;
        tabOverView.InitializeActiveTab(files);
        InitializeNewTab(tab);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    /// <summary>
    /// Applies the docked-gallery settings to a tab before it is initialized: undocks when the interface is hidden and
    /// the docked gallery should not show then, and opens a docked gallery without animation.
    /// </summary>
    /// <remarks>Call before <see cref="Initialize(CoreViewModel, List{FileInfo}, MainWindowViewModel)"/>, which subscribes to
    /// the gallery mode; changing it afterwards would hide the docked gallery.</remarks>
    /// <returns>Whether the docked gallery is shown and needs loading.</returns>
    public static bool PrepareDockedGallery(TabViewModel tab)
    {
        var gallery = tab.Gallery;
        if (Settings.Gallery.IsGalleryDocked)
        {
            if (!Settings.UIProperties.ShowInterface && !Settings.Gallery.ShowDockedGalleryInHiddenUI)
            {
                gallery.IsGalleryDocked.Value = false;
                return false;
            }
            if (Settings.Gallery.DockPosition is GalleryDockPosition.Closed)
            {
                Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
            }
            gallery.ActiveGalleryMode.Value = GalleryMode.DockedNoAnimation;
            return true;
        }
        Settings.Gallery.DockPosition = GalleryDockPosition.Closed;
        return false;
    }

    /// <summary>
    /// Gives an initialized tab a new, empty image iterator from the window's shared services, for a tab whose
    /// iterator was disposed to show a single image. Does not subscribe to the tab again.
    /// </summary>
    public static void RecreateImageIterator(TabOverviewViewModel tabs, TabViewModel tab)
    {
        if (tabs.SharedCache is not { } cache ||
            tabs.SharedThumbnailCache is not { } thumbCache ||
            tabs.SharedThumbnailLoader is not { } thumbLoader)
        {
            return;
        }

        tab.InitializeImageIterator([], cache, thumbCache, thumbLoader, tabs.SharedFileWatcher, thumbCache);
    }

    /// <summary>
    /// Sets up navigation for every Main Window the first time any tab opens a source; does nothing after that.
    /// </summary>
    private static void InitializeNavigation(CoreViewModel core)
    {
        if (core.SharedNavigationService is not null)
        {
            return;
        }

        Debug.Assert(core.PlatformService != null);
        // The shared image cache loads through the same logic as AvaloniaImageLoader (via GetImageModel)
        var thumbnailLoader = ServiceHelper.ThumbLoader;
        var fileWatcher = new FileWatcherService(core.PlatformService.CompareStrings, core.SharedCache,
            core.SharedThumbnailCache);
        var navigation = new NavigationService(ServiceHelper.ImageLoader, core.SharedCache, fileWatcher,
            core.PlatformService, core.PlatformService.CompareStrings);
        core.InitializeNavigation(navigation, thumbnailLoader, fileWatcher);
    }

    public static void InitializeConsecutiveTab(CoreViewModel core, TabViewModel tab)
    {
        InitializeNewTab(tab);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    private static void InitializeNewTab(TabViewModel newTab)
    {
        if (newTab is null)
        {
            return;
        }
        if (newTab.IsInitialized)
        {
#if DEBUG
            DebugHelper.LogDebug(nameof(TabNavigationInitializer), nameof(InitializeNewTab), $"Tab {newTab.Id} {newTab.Model?.FileInfo?.Name} is already initialized");
#endif
            return;
        }

        newTab.ParentWindowContext.WindowTabs.HookUp(newTab);
    }
}
