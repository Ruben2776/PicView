using System.Diagnostics;
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

        var tabOverView = vm.WindowTabs;
        var tab = tabOverView.ActiveTab.CurrentValue;
        tabOverView.InitializeActiveTab(files);
        InitializeNewTab(tab);
        tab.Gallery.Initialize(core.GallerySettings);
        core.GallerySettings.Initialize();
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

    public static void InitializeConsecutiveTab(CoreViewModel core, TabViewModel tab)
    {
        InitializeNewTab(tab);
        tab.Gallery.Initialize(core.GallerySettings);
        core.GallerySettings.Initialize();
    }
    
    private static void InitializeNewTab(TabViewModel newTab)
    {
        if (newTab is null)
        {
            return;
        }
        // The startup path hooks the tab up before its first image shows
        if (newTab.IsInitialized)
        {
            return;
        }

        newTab.ParentWindowContext.WindowTabs.HookUp(newTab);
    }
}
