using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Opens the files after the first of a paste or a drop of several, in the background, as the Opening Mode says: under
/// New Tab each in a new tab at the end of the window, leaving its active tab active; otherwise each in a new Main
/// Window of this PicView, opened from that window.
/// </summary>
internal static class OtherFilesOpener
{
    /// <returns>Completes once every file has opened or failed.</returns>
    internal static async Task OpenAsync(MainWindow mainWindow, CoreViewModel core, IEnumerable<string> paths,
        OpeningMode mode)
    {
        var inWindows = OpeningTarget.OpensOthersInWindows(mode);
        var lifecycle = mainWindow.Lifecycle;
        if (inWindows && lifecycle is null)
        {
            return;
        }

        var tabs = mainWindow.ViewModel!.WindowTabs;
        var opening = new List<Task>();
        foreach (var path in paths)
        {
            try
            {
                if (inWindows)
                {
                    await lifecycle!.OpenNewAsync(mainWindow, path).ConfigureAwait(false);
                    continue;
                }

                // Made on the UI thread, where its views are attached
                var tab = await Dispatcher.UIThread.InvokeAsync(() => tabs.InsertTab(tabs.Tabs.Value.Count));
                opening.Add(SourceOpener.OpenAsync(mainWindow, core, path, tab: tab).AsTask());
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(OtherFilesOpener), nameof(OpenAsync), e);
            }
        }

        try
        {
            await Task.WhenAll(opening).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(OtherFilesOpener), nameof(OpenAsync), e);
        }
    }
}
