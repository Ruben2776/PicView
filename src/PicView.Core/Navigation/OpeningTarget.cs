using PicView.Core.Config;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// Where a pasted, dropped or handed-over Source opens, as the Opening Mode says.
/// </summary>
public static class OpeningTarget
{
    /// <summary>
    /// Makes the tab a Source opens in under New Tab: a new one at the end of the strip, which becomes active, unless
    /// the active tab is a Fresh Tab, which the Source then opens in. Call on the UI thread, before the Source opens,
    /// so what opens it into the active tab opens it into this one.
    /// </summary>
    /// <returns>The tab it made, or null when the Source opens in the active tab.</returns>
    public static TabViewModel? PrepareNewTab(TabOverviewViewModel tabs, OpeningMode mode)
    {
        if (mode != OpeningMode.NewTab || tabs.ActiveTab.Value.IsFresh)
        {
            return null;
        }

        return tabs.CreateTab();
    }

    /// <summary>
    /// Whether the other files of a paste or a drop of several open in new Main Windows rather than in new tabs: they do
    /// unless the mode is New Tab.
    /// </summary>
    public static bool OpensOthersInWindows(OpeningMode mode) => mode != OpeningMode.NewTab;
}
