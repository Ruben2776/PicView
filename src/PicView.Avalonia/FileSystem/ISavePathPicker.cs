namespace PicView.Avalonia.FileSystem;

/// <summary>
/// Asks the user where to save a file. Only picks a path; what gets written there is up to the caller.
/// </summary>
public interface ISavePathPicker
{
    /// <summary>
    /// Lets the user pick a destination path for saving.
    /// </summary>
    /// <param name="suggestedName">The file name, or path, to suggest.</param>
    /// <param name="ext">An extension to suggest instead of the one in <paramref name="suggestedName"/>.</param>
    /// <returns>The picked path, or <see langword="null"/> when the user cancelled.</returns>
    Task<string?> PickSavePathAsync(string? suggestedName, string? ext = null);
}
