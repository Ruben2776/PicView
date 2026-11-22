using PicView.Core.Config;
using PicView.Core.FileHistory;

namespace PicView.Core.Navigation;

/// <summary>
/// What App Start opens: the start-up menu, the Last File, or the Last Session. A pure decision, made before any
/// window is opened.
/// </summary>
/// <param name="ActiveSource">What the active tab shows, or null for the start-up menu.</param>
/// <param name="Sources">What each tab shows, in tab order; empty for the start-up menu. Only the active one is known
/// to exist.</param>
/// <param name="ActiveIndex">The index in <paramref name="Sources" /> of the active tab.</param>
public readonly record struct StartUpPlan(string? ActiveSource, IReadOnlyList<string> Sources, int ActiveIndex)
{
    /// <summary>Whether App Start shows the start-up menu.</summary>
    public bool ShowsMenu => ActiveSource is null;

    /// <summary>Whether more tabs than the active one are opened.</summary>
    public bool HasOtherTabs => Sources.Count > 1;

    /// <summary>The plan that shows the start-up menu.</summary>
    public static StartUpPlan Menu { get; } = new(null, [], 0);

    /// <summary>
    /// Decides what App Start opens. The Last File is opened without a check that it exists. Of a Last Session only the
    /// active source is checked; if it is gone, the nearest remaining source after it, else before it, is checked in
    /// turn, one at a time, and the first that exists becomes active. Those found gone are dropped, and every other
    /// source is left for the restore to check, off the UI thread. A URL is never checked.
    /// </summary>
    /// <param name="exists">Whether a local path exists. It may block for long on a network path, so it is called as
    /// few times as can be.</param>
    public static StartUpPlan Decide(StartUpBehavior behavior, bool historyEnabled, string? lastFile,
        LastSession? session, Func<string, bool> exists)
    {
        if (!historyEnabled)
        {
            return Menu;
        }

        switch (behavior)
        {
            case StartUpBehavior.OpenLastFile:
                return string.IsNullOrWhiteSpace(lastFile) ? Menu : new StartUpPlan(lastFile, [lastFile], 0);
            case StartUpBehavior.ContinueSession when session is { Sources.Count: > 0 }:
                return ContinueSession(session, exists);
            default:
                return Menu;
        }
    }

    private static StartUpPlan ContinueSession(LastSession session, Func<string, bool> exists)
    {
        var sources = session.Sources;
        var count = sources.Count;
        var active = session.ActiveIndex is >= 0 && session.ActiveIndex < count ? session.ActiveIndex : 0;

        // Checked in the order LastSession.RemapIndex picks: the active one, then each after it, then each before it,
        // nearest first. Those passed over are the ones found gone
        var found = -1;
        var gone = new HashSet<int>();
        foreach (var i in WalkOrder(active, count))
        {
            if (IsUrl(sources[i]) || exists(sources[i]))
            {
                found = i;
                break;
            }

            gone.Add(i);
        }

        if (found < 0)
        {
            return Menu;
        }

        var kept = new List<string>(count - gone.Count);
        var activeIndex = 0;
        for (var i = 0; i < count; i++)
        {
            if (gone.Contains(i))
            {
                continue;
            }

            if (i == found)
            {
                activeIndex = kept.Count;
            }

            kept.Add(sources[i]);
        }

        return new StartUpPlan(sources[found], kept, activeIndex);
    }

    private static IEnumerable<int> WalkOrder(int active, int count)
    {
        for (var i = active; i < count; i++)
        {
            yield return i;
        }

        for (var i = active - 1; i >= 0; i--)
        {
            yield return i;
        }
    }

    /// <summary>Whether a source is a URL, which is opened without a check that it exists.</summary>
    public static bool IsUrl(string source) =>
        source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
