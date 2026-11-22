using PicView.Core.Config;
using PicView.Core.FileHistory;

namespace PicView.Core.Navigation;

/// <summary>
/// What App Start opens: the start-up menu, the Last File, or the Last Session. A pure decision, made before any
/// window is opened.
/// </summary>
/// <param name="ActiveSource">What the active tab shows, or null for the start-up menu.</param>
/// <param name="Sources">What each tab shows, in tab order; empty for the start-up menu.</param>
/// <param name="ActiveIndex">The index in <paramref name="Sources" /> of the active tab.</param>
public readonly record struct StartUpPlan(string? ActiveSource, IReadOnlyList<string> Sources, int ActiveIndex)
{
    /// <summary>Whether App Start shows the start-up menu.</summary>
    public bool ShowsMenu => ActiveSource is null;

    /// <summary>Whether more tabs than the active one are opened.</summary>
    public bool HasOtherTabs => Sources.Count > 1;

    private static StartUpPlan Menu => new(null, [], 0);

    /// <summary>
    /// Decides what App Start opens. The Last File is opened without a check that it exists; a Last Session drops each
    /// local path that no longer exists, and keeps every URL.
    /// </summary>
    /// <param name="exists">Whether a local path exists.</param>
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
                var kept = new List<bool>(session.Sources.Count);
                var sources = new List<string>();
                foreach (var source in session.Sources)
                {
                    var keep = IsUrl(source) || exists(source);
                    kept.Add(keep);
                    if (keep)
                    {
                        sources.Add(source);
                    }
                }

                if (sources.Count == 0)
                {
                    return Menu;
                }

                var active = LastSession.RemapIndex(kept, session.ActiveIndex);
                return new StartUpPlan(sources[active], sources, active);
            default:
                return Menu;
        }
    }

    private static bool IsUrl(string source) =>
        source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
