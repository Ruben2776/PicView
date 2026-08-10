using System.Diagnostics;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Navigation.Services;
using PicView.Core.DebugTools;
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
        core.SharedNavigationService ??= new NavigationService(imageLoader, sharedCache, fileWatcher, core.PlatformService, thumbnailService, core.PlatformService.CompareStrings);

        // 4. Initialize ViewModel
        Debug.Assert(core.MainWindows.ActiveWindow.CurrentValue != null);
        var tabOverView = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs;
        var tab = tabOverView.ActiveTab.CurrentValue;
        tabOverView.LoadAndInitializeFromPath(files, core.SharedNavigationService, sharedCache, thumbnailCache, thumbnailService, fileWatcher);
        tabOverView.SetParentContext(core.MainWindows.ActiveWindow.CurrentValue);
        InitializeNewTab(tab, core.MainWindows.ActiveWindow.CurrentValue, mainWindow);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    public static void InitializeConsecutiveTab(CoreViewModel core, MainWindow mainWindow, TabViewModel tab)
    {
        InitializeNewTab(tab, core.MainWindows.ActiveWindow.CurrentValue, mainWindow);
        tab.Gallery.Initialize();
        core.GallerySettings.Initialize();
    }
    
    public static void InitializeDetachedWindow(MainWindow mainWindow, MainWindowViewModel parentVm, MainWindowViewModel newVm, TabViewModel tab)
    {
        newVm.WindowTabs.Tabs.Value[0] = tab;
        
        // Initialize the NEW window's tabs with the OLD window's services
        // This ensures both windows share the same memory cache
        if (parentVm.WindowTabs.SharedCache is not { } cache ||
            parentVm.WindowTabs.SharedThumbnailCache is not { } thumbCache || 
            parentVm.WindowTabs.SharedNavigation is not { } nav ||
            parentVm.WindowTabs.SharedThumbnailLoader is not { } thumbLoader ||
            parentVm.WindowTabs.SharedFileWatcher is not { } fileWatcher)
        {
            return;
        }
        
        if (tab.FileInfo.CurrentValue is not null)
        {
            newVm.WindowTabs.LoadAndInitializeFromPath(tab.ImageIterator.Files, nav, cache, thumbCache, thumbLoader, fileWatcher);
            newVm.WindowTabs.ActiveTab.CurrentValue.UpdateTabTitle();
        }
        else
        {
            newVm.WindowTabs.ActiveTab.CurrentValue.SetNewTabTitle();
            
            newVm.WindowTabs.LoadAndInitialize(nav, cache, thumbCache, thumbLoader, fileWatcher);
            newVm.WindowTabs.SetParentContext(newVm);
        }
        newVm.WindowTabs.SelectTab(tab);
        tab.ImageIterator.UpdateNavigationProperties();
        
        // Unsubscribe from the old and start listening to a new one
        tab.Disposables.Clear();
        NavigationSubscriptions.ModelSubscription(tab, newVm, mainWindow);
        
        // Need to properly remove it from the previous location
        parentVm.WindowTabs.RemoveTab(tab);

        if (Settings.WindowProperties.AutoFit)
        {
            mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
        }
    }
    
    private static void InitializeNewTab(TabViewModel newTab, MainWindowViewModel mainWindowViewModel, MainWindow mainWindow)
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

        NavigationSubscriptions.ModelSubscription(newTab, mainWindowViewModel, mainWindow);
        newTab.IsInitialized = true;
    }
}