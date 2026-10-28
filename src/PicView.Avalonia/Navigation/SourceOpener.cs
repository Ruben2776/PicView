using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// How <see cref="SourceOpener.OpenAsync"/> opens a source.
/// </summary>
/// <param name="IsStartup">The window is not shown yet; open it in the startup order (see ADR 0005).</param>
/// <param name="ContinueFromLeftOff">
/// List files from the remembered start-up directory when browsing subdirectories, and remember it again.
/// </param>
public readonly record struct OpenOptions(bool IsStartup = false, bool ContinueFromLeftOff = false);

/// <summary>
/// The one way the UI opens a file, directory, archive or URL in the active tab of a window: at startup, into a tab
/// that has not been initialized yet, into one showing a single image, or into one already browsing.
/// </summary>
/// <remarks>
/// Loading itself is Core's (<see cref="TabOverviewViewModel.LoadFromStringAsync"/>); this module owns what the UI
/// does around it: the startup order, initializing a fresh tab, switching from the start-up menu, the loading
/// indicator, and telling the user when a source could not be opened.
/// </remarks>
public static class SourceOpener
{
    public static async ValueTask<LoadResult> OpenAsync(MainWindow mainWindow, CoreViewModel core, string source,
        OpenOptions options = default)
    {
        var vm = mainWindow.ViewModel!;
        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        if (options.IsStartup && !tab.IsInitialized)
        {
            return await QuickLoad.StartUpAsync(mainWindow, core, source, options.ContinueFromLeftOff).ConfigureAwait(false);
        }

        if (!tab.IsInitialized)
        {
            // Prepare the gallery before initializing, so the docked gallery stays visible and loads with the files
            TabNavigationInitializer.PrepareDockedGallery(tab);
            TabNavigationInitializer.Initialize(core, [], vm);
        }
        else if (tab.ImageIterator is null)
        {
            // Showing a single image (clipboard, base64) disposed the iterator, browse files with a new one
            TabNavigationInitializer.PrepareDockedGallery(tab);
            TabNavigationInitializer.RecreateImageIterator(tabs, tab);
        }

        var wasStartUpMenu = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (tab.CurrentView.CurrentValue is not StartUpMenu)
            {
                return false;
            }
            tab.CurrentView.Value = new ImageViewer();
            return true;
        });

        var rootDirectory = options.ContinueFromLeftOff ? GetStartUpDirectory(source) : null;
        vm.IsLoadingIndicatorShown.Value = true;
        LoadResult result;
        try
        {
            result = await tabs.LoadFromStringAsync(source, rootDirectory: rootDirectory).ConfigureAwait(false);
            if (Settings.WindowProperties.AutoFit)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.WindowFitter.Center();
                });
            }
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }

        if (!result.IsLoaded)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowError(vm, result);
                if (wasStartUpMenu)
                {
                    tab.CurrentView.Value = new StartUpMenu();
                }
            });
            return result;
        }

        if (rootDirectory is not null)
        {
            Settings.StartUp.StartUpDirectory = rootDirectory;
        }
        ShowHoverBarIfNeeded(tab);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tab.UpdateTabTitle();
            mainWindow.WindowFitter.Refit();
        });
        return result;
    }

    /// <summary>
    /// Tells the user why a source could not be opened, when there is a reason to tell. Call on the UI thread.
    /// </summary>
    internal static void ShowError(MainWindowViewModel vm, LoadResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            TooltipHelper.ShowTooltipMessage(vm, result.Error, center: true, TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// The directory to list files from when continuing from where the user left off: the remembered start-up
    /// directory if browsing subdirectories and the file is inside it, else the file's own directory, or
    /// <see langword="null"/> when not browsing subdirectories or the source is not a file.
    /// </summary>
    internal static string? GetStartUpDirectory(string source)
    {
        if (!Settings.Sorting.IncludeSubDirectories || !File.Exists(source))
        {
            return null;
        }

        var remembered = Settings.StartUp.StartUpDirectory;
        return !string.IsNullOrWhiteSpace(remembered) && source.Contains(remembered, StringComparison.Ordinal)
            ? remembered
            : Path.GetDirectoryName(source);
    }

    internal static void ShowHoverBarIfNeeded(TabViewModel tab)
    {
        if (!Settings.UIProperties.ShowInterface && Settings.UIProperties.ShowAltInterfaceButtons
            || Settings.UIProperties.ShowAltInterfaceButtons &&
            tab.ParentWindowContext.WindowModeState.CurrentValue.Mode is WindowMode.Fullscreen)
        {
            tab.Hoverbar.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
        }
        else
        {
            tab.Hoverbar.IsHoverbarVisible.Value = false;
        }
    }
}
