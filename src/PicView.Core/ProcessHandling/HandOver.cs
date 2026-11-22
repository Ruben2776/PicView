using System.Diagnostics;
using PicView.Core.Config;

namespace PicView.Core.ProcessHandling;

/// <summary>
/// What the running PicView does with a hand-over.
/// </summary>
public enum HandOverResponse
{
    /// <summary>Does not take it, so the PicView that sent it starts on its own.</summary>
    Refuse,

    /// <summary>Opens the Source in the Active Main Window and brings that window to the front.</summary>
    OpenInActiveMainWindow,

    /// <summary>Brings the Active Main Window to the front and opens nothing.</summary>
    ActivateActiveMainWindow,

    /// <summary>Opens a new Main Window that shows the Source, or a Fresh Tab when there is none.</summary>
    OpenInNewMainWindow,

    /// <summary>
    /// Opens the Source in a new tab of the Active Main Window, or in its active tab when that is a Fresh Tab, and
    /// brings that window to the front.
    /// </summary>
    OpenInNewTab
}

/// <summary>
/// Decides what the running PicView does with the Source another PicView process handed over (ADR-0037).
/// </summary>
public static class HandOverReceipt
{
    /// <param name="isShuttingDown">Whether App Shutdown has begun.</param>
    /// <param name="mode">The Opening Mode.</param>
    /// <param name="hasSource">Whether the other PicView was started with a Source.</param>
    /// <param name="hasMainWindow">Whether a Main Window is open; none is while only Batch Resize is.</param>
    public static HandOverResponse Respond(bool isShuttingDown, OpeningMode mode, bool hasSource,
        bool hasMainWindow)
    {
        if (isShuttingDown)
        {
            return HandOverResponse.Refuse;
        }

        if (mode == OpeningMode.NewWindow || !hasMainWindow)
        {
            return HandOverResponse.OpenInNewMainWindow;
        }

        if (!hasSource)
        {
            return HandOverResponse.ActivateActiveMainWindow;
        }

        return mode == OpeningMode.NewTab ? HandOverResponse.OpenInNewTab : HandOverResponse.OpenInActiveMainWindow;
    }
}

/// <summary>
/// The lines of a hand-over. The starting PicView sends one, <c>new</c> or <c>open</c> and its Source; the running one
/// answers <c>ready</c> or <c>refused</c>; and the starting one sends <c>confirmed</c> once it has read <c>ready</c>,
/// after which the Source is the running PicView's to open.
/// </summary>
public static class HandOverMessage
{
    private const string WithoutSource = "new";
    private const string WithSource = "open ";

    /// <summary>The answer of a PicView that will open the Source once it is confirmed.</summary>
    internal const string Ready = "ready";

    /// <summary>The answer of a PicView that will not, as once App Shutdown has begun.</summary>
    internal const string Refused = "refused";

    /// <summary>What the starting PicView sends once it has read <see cref="Ready"/>, before it exits.</summary>
    internal const string Confirmed = "confirmed";

    public static string Write(string? source) => source is null ? WithoutSource : WithSource + source;

    /// <summary>
    /// Whether the one-line message can carry <paramref name="source"/>. One with a line break, which a file name may
    /// have outside Windows, would arrive cut short, so the PicView started with it starts normally instead.
    /// </summary>
    public static bool CanCarry(string? source) => source is null || source.AsSpan().IndexOfAny('\n', '\r') < 0;

    /// <param name="source">The Source that was handed over, or null when there was none.</param>
    /// <returns>Whether <paramref name="line"/> is a hand-over.</returns>
    public static bool TryRead(string? line, out string? source)
    {
        source = null;
        if (string.Equals(line, WithoutSource, StringComparison.Ordinal))
        {
            return true;
        }

        if (line is null || !line.StartsWith(WithSource, StringComparison.Ordinal))
        {
            return false;
        }

        source = line[WithSource.Length..];
        return true;
    }

    /// <summary>
    /// The Source as the running PicView can find it: a file or folder given relative to the folder the new PicView was
    /// started in gets its full path, since the running one was started somewhere else. Anything else, such as a URL,
    /// is left as it is.
    /// </summary>
    public static string? AbsoluteSource(string? source, string startedIn)
    {
        if (string.IsNullOrWhiteSpace(source) || Path.IsPathFullyQualified(source))
        {
            return source;
        }

        try
        {
            var full = Path.GetFullPath(source, startedIn);
            return File.Exists(full) || Directory.Exists(full) ? full : source;
        }
        catch (Exception)
        {
            // Not a path, as a URL with characters no path has
            return source;
        }
    }
}

/// <summary>
/// The pipe hand-overs go through.
/// </summary>
public static class HandOverPipe
{
    /// <summary>
    /// The name of the pipe: one per user, and on Windows one per sign-in session, so a PicView never hands over to
    /// another user's, nor to one on a desktop the user is not looking at. A Debug build has its own, so a PicView
    /// under development and an installed one leave each other alone.
    /// </summary>
    public static string Name { get; } = BuildName();

    /// <summary>
    /// The file a PicView locks to be the one that listens, where the pipe is a socket file that cannot decide it:
    /// everywhere but Windows. It is in the user's runtime folder when there is one, else in the temp folder.
    /// </summary>
    public static string LockFile { get; } = Path.Combine(LockFolder(), Name + ".lock");

    private static string LockFolder()
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        return !string.IsNullOrWhiteSpace(runtime) && Directory.Exists(runtime) ? runtime : Path.GetTempPath();
    }

    private static string BuildName()
    {
        var name = "PicView-HandOver-" + Environment.UserName;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                name += "-" + process.SessionId;
            }
            catch (Exception)
            {
                // The user's name alone still keeps users apart
            }
        }
#if DEBUG
        name += "-Debug";
#endif
        return name;
    }
}
