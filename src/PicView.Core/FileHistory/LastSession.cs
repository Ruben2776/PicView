using PicView.Core.ViewModels;

namespace PicView.Core.FileHistory;

/// <summary>
///     The Last Session: what was open in the Active Main Window when the app closed. It is kept in the file history
///     file, so it follows the File History setting.
/// </summary>
public sealed class LastSession
{
    /// <summary>The path or URL each open tab shows, in tab order.</summary>
    public List<string> Sources { get; set; } = [];

    /// <summary>The index in <see cref="Sources" /> of the tab that was active.</summary>
    public int ActiveIndex { get; set; }

    /// <summary>
    ///     The Last Session of a window's tabs. A tab that shows nothing with a path is left out, and the active index
    ///     follows what is kept: the active tab's own, else the nearest kept tab after it, else before it.
    /// </summary>
    public static LastSession Of(IReadOnlyList<TabViewModel> tabs, TabViewModel active)
    {
        var kept = new List<bool>(tabs.Count);
        var session = new LastSession();
        var activeIndex = 0;
        for (var i = 0; i < tabs.Count; i++)
        {
            var path = tabs[i].ViewHistory.Current?.HistoryPath;
            kept.Add(path is not null);
            if (path is not null)
            {
                session.Sources.Add(path);
            }

            if (ReferenceEquals(tabs[i], active))
            {
                activeIndex = i;
            }
        }

        session.ActiveIndex = session.Sources.Count > 0 ? RemapIndex(kept, activeIndex) : 0;
        return session;
    }

    /// <summary>
    ///     The index among the kept items of the item at <paramref name="index" />; if it was dropped, the nearest kept
    ///     item after it, else before it, else 0.
    /// </summary>
    internal static int RemapIndex(IReadOnlyList<bool> kept, int index)
    {
        if (index < 0 || index >= kept.Count)
        {
            index = 0;
        }

        var before = 0;
        for (var i = 0; i < index; i++)
        {
            if (kept[i])
            {
                before++;
            }
        }

        if (kept[index])
        {
            return before;
        }

        for (var i = index + 1; i < kept.Count; i++)
        {
            if (kept[i])
            {
                return before;
            }
        }

        return Math.Max(before - 1, 0);
    }
}
