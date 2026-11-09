using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
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
/// <param name="ArchiveEntry">For an archive, the entry to open it at instead of its first.</param>
public readonly record struct OpenOptions(bool IsStartup = false, bool ContinueFromLeftOff = false,
    string? ArchiveEntry = null);

/// <summary>
/// The one way the UI opens a file, directory, archive or URL in a tab of a window, its active one unless told which:
/// at startup, into a tab that has not been opened yet, into one showing a single image, or into one already browsing.
/// </summary>
/// <remarks>
/// Loading itself is the tab's (<see cref="TabViewModel.Load"/>); this module owns what the UI does around it: the
/// startup order, hooking a fresh tab up, and telling the user when a source could not be opened.
/// </remarks>
public static class SourceOpener
{
    /// <param name="tab">One of <paramref name="mainWindow"/>'s tabs to open the source in; its active tab when null.</param>
    public static async ValueTask<LoadResult> OpenAsync(MainWindow mainWindow, CoreViewModel core, string source,
        OpenOptions options = default, TabViewModel? tab = null)
    {
        var vm = mainWindow.ViewModel!;
        tab ??= vm.WindowTabs.ActiveTab.CurrentValue;
        if (options.IsStartup && !tab.IsInitialized)
        {
            return await QuickLoad.StartUpAsync(mainWindow, core, source, options.ContinueFromLeftOff).ConfigureAwait(false);
        }

        if (!tab.IsInitialized)
        {
            TabNavigationInitializer.Initialize(core, vm, tab);
        }

        var rootDirectory = options.ContinueFromLeftOff ? GetStartUpDirectory(source) : null;
        var result = await tab.Load.LoadAsync(source,
            new LoadOptions(RootDirectory: rootDirectory, ArchiveEntry: options.ArchiveEntry)).ConfigureAwait(false);
        // A newer load overtook this one and owns the view, so this one changes nothing
        if (result.Outcome is LoadOutcome.Superseded)
        {
            return result;
        }

        if (result.Outcome is LoadOutcome.Failed)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowError(vm, result);
            });
            return result;
        }

        if (rootDirectory is not null)
        {
            Settings.StartUp.StartUpDirectory = rootDirectory;
        }
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
}
