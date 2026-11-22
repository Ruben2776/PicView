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
}
