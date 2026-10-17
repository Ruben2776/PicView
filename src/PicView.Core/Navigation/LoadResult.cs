namespace PicView.Core.Navigation;

/// <summary>
/// The outcome of loading a source into a tab.
/// </summary>
/// <param name="IsLoaded">Whether the source was loaded.</param>
/// <param name="Error">Why loading failed, when there is something to tell the user (e.g., a failed download).</param>
public readonly record struct LoadResult(bool IsLoaded, string? Error = null)
{
    public static LoadResult Loaded { get; } = new(true);

    public static LoadResult Failed(string? error = null) => new(false, error);
}
