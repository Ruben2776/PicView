using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// The UI side of a tab's first open. The tab's Load makes its iterator and lists its files.
/// </summary>
public static class TabNavigationInitializer
{
    /// <summary>
    /// Opens <paramref name="tab"/>, one of <paramref name="vm"/>'s tabs, for the first time: it starts in the gallery
    /// mode the settings remember, without animation, is hooked up to its window, and its gallery takes the shared
    /// gallery settings. May be called from any thread.
    /// </summary>
    public static void Initialize(CoreViewModel core, MainWindowViewModel vm, TabViewModel tab)
    {
        tab.Gallery.Switcher.Start(Settings.Gallery.IsGalleryDocked);
        vm.WindowTabs.HookUp(tab);
        InitializeGallery(core, tab);
    }

    /// <summary>
    /// Gives a tab opened for the first time the shared gallery settings, for the startup path, which starts the
    /// gallery mode and hooks the tab up itself before its first image shows.
    /// </summary>
    internal static void InitializeGallery(CoreViewModel core, TabViewModel tab)
    {
        tab.Gallery.Initialize(core.GallerySettings);
        core.GallerySettings.Initialize();
    }
}
