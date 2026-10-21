using System.Diagnostics;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation.Services;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

public static class TabNavigationInitializer
{
    public static void Initialize(CoreViewModel core, FileInfo fileInfo, MainWindow mainWindow)
    {
        Debug.Assert(core.PlatformService != null);
        Initialize(core, core.PlatformService.GetFiles(fileInfo), mainWindow);
    }
    
    public static void Initialize(CoreViewModel core, List<FileInfo> files, MainWindow mainWindow)
    {
        // --- Initialization Logic ---
        // This is the initialization logic for the navigation system.
        // It is initialized after initial image load, to make it feel faster by showing the image asap. 
        
        // 1. Create dependencies
        var imageLoader = ServiceHelper.ImageLoader;
        var thumbnailService = ServiceHelper.ThumbLoader;

        // 2. Create SharedImageCache
        // We use the same loading logic as AvaloniaImageLoader (via GetImageModel)
        var sharedCache = core.SharedCache;
        var thumbnailCache = core.SharedThumbnailCache;

        Debug.Assert(core.PlatformService != null);
        var fileWatcher = new FileWatcherService(core.PlatformService.CompareStrings, sharedCache, thumbnailCache, thumbnailService);

        // 3. Create NavigationService (Core)
        core.SharedNavigationService ??= new NavigationService(imageLoader, sharedCache, fileWatcher, core.PlatformService, core.PlatformService.CompareStrings);

        // 4. Initialize ViewModel
        Debug.Assert(core.MainWindows.ActiveWindow.CurrentValue != null);
        var tabOverView = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs;
        var tab = tabOverView.ActiveTab.CurrentValue;
        tabOverView.LoadAndInitializeFromPath(files, core.SharedNavigationService, sharedCache, thumbnailCache, thumbnailService, fileWatcher);
        InitializeNewTab(tab);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    /// <summary>
    /// Applies the docked-gallery settings to a tab before it is initialized: undocks when the interface is hidden and
    /// the docked gallery should not show then, and opens a docked gallery without animation.
    /// </summary>
    /// <remarks>Call before <see cref="Initialize(CoreViewModel, List{FileInfo}, MainWindow)"/>, which subscribes to
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

    public static void InitializeConsecutiveTab(CoreViewModel core, MainWindow mainWindow, TabViewModel tab)
    {
        InitializeNewTab(tab);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    /// <summary>
    /// Sets up a Main Window that opened with a tab moved out of <paramref name="from"/>: it shares that window's
    /// services, so the tabs it opens share the same caches, and the moved tab gets its title and navigation back.
    /// </summary>
    public static void InitializeMovedTabWindow(MainWindowViewModel newVm, TabOverviewViewModel from, TabViewModel tab)
    {
        newVm.WindowTabs.ShareServicesOf(from);
        if (tab.FileInfo.CurrentValue is not null)
        {
            tab.UpdateTabTitle();
        }
        else
        {
            tab.SetNewTabTitle();
        }
        tab.ImageIterator?.UpdateNavigationProperties();
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
