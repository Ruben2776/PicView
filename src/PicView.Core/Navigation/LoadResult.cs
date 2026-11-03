using System.Runtime.InteropServices;

namespace PicView.Core.Navigation;

/// <summary>
/// The outcome of loading a source into a tab.
/// </summary>
/// <param name="Outcome">Whether the source was loaded, a newer load superseded it, or it failed.</param>
/// <param name="Error">Why loading failed, when there is something to tell the user (e.g., a failed download).</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct LoadResult(LoadOutcome Outcome, string? Error = null)
{
    /// <summary>Whether the source was loaded.</summary>
    public bool IsLoaded => Outcome is LoadOutcome.Loaded;

    public static LoadResult Loaded { get; } = new(LoadOutcome.Loaded);

    /// <summary>A newer load of the tab began before this one finished, which is no failure to tell the user of.</summary>
    public static LoadResult Superseded { get; } = new(LoadOutcome.Superseded);

    public static LoadResult Failed(string? error = null) => new(LoadOutcome.Failed, error);
}
