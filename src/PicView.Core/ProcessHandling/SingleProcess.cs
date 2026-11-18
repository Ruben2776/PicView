using Microsoft.Win32.SafeHandles;

namespace PicView.Core.ProcessHandling;

/// <summary>
/// How a PicView process starts.
/// </summary>
public enum AppStartKind
{
    /// <summary>A running PicView took the Source, so this process exits without a window.</summary>
    HandedOver,

    /// <summary>No PicView was listening: this one runs App Start and takes the hand-overs of later ones.</summary>
    Listening,

    /// <summary>Another PicView is the one that listens and did not take the Source: this one runs App Start as its
    /// own process.</summary>
    Alone
}

/// <summary>
/// Keeps PicView to one process: a PicView that starts while another is running hands its Source over to it and exits,
/// and the running one opens it (ADR-0037).
/// </summary>
public static class SingleProcess
{
    /// <summary>
    /// The argument PicView starts itself with when it restarts: the new process hands nothing over and runs its own
    /// App Start, whichever PicView is listening. It comes before the Source and is not one.
    /// </summary>
    public const string OwnProcessArgument = "--own-process";

    // A PicView that listens answers from a pool thread at once, whatever its windows are doing
    private static readonly TimeSpan AcknowledgeTimeout = TimeSpan.FromMilliseconds(1500);

    // For the second attempt, at a PicView that is known to hold the pipe
    private static readonly TimeSpan PatientConnectTimeout = TimeSpan.FromMilliseconds(1000);

    // For a PicView that started alone: how often, and how many times, it asks to be the one that listens
    private static readonly TimeSpan RetryListening = TimeSpan.FromSeconds(1);
    private const int RetryListeningAttempts = 30;

    /// <summary>
    /// Takes the hand-overs of the PicViews started after this one. Null when this process does not take part: a
    /// file-association start, or a platform that brings a second start to the running PicView itself.
    /// </summary>
    public static HandOverListener? Listener { get; private set; }

    /// <summary>
    /// Run first thing in the process, before any window: hands the Source this PicView was started with over to a
    /// running PicView, or else starts listening for the hand-overs of later ones.
    /// </summary>
    /// <param name="args">The command-line arguments as <c>Main</c> gets them, without the program: the Source, if
    /// any, is the first that is not <see cref="OwnProcessArgument"/>.</param>
    /// <param name="connected">Called with the pipe to the running PicView, before the Source is sent.</param>
    /// <returns>True when a running PicView took the Source and this process should exit.</returns>
    public static bool HandOverAtStart(IReadOnlyList<string> args, Action<SafePipeHandle>? connected = null)
    {
        var handedOver = HandOverAtStart(args, HandOverPipe.Name,
            OperatingSystem.IsWindows() ? null : HandOverPipe.LockFile, Environment.CurrentDirectory,
            PatientConnectTimeout, AcknowledgeTimeout, connected, out var listener);
        Listener = listener;
        return handedOver;
    }

    /// <param name="args">The command-line arguments, without the program.</param>
    /// <param name="pipeName">The pipe hand-overs go through.</param>
    /// <param name="lockFile">The file the listening PicView locks, or null where the pipe decides it.</param>
    /// <param name="startedIn">The folder a relative Source is relative to.</param>
    /// <param name="patientConnectTimeout">How long the second attempt waits for the pipe.</param>
    /// <param name="acknowledgeTimeout">How long each attempt waits for an answer.</param>
    /// <param name="connected">Called with the pipe to the running PicView, before the Source is sent.</param>
    /// <param name="listener">Takes later hand-overs, listening already or still asking to. Null when this process
    /// handed over or takes no part.</param>
    internal static bool HandOverAtStart(IReadOnlyList<string> args, string pipeName, string? lockFile,
        string startedIn, TimeSpan patientConnectTimeout, TimeSpan acknowledgeTimeout,
        Action<SafePipeHandle>? connected, out HandOverListener? listener)
    {
        listener = null;
        var source = SourceOf(args);
        if (IsExempt(source))
        {
            return false;
        }

        source = HandOverMessage.AbsoluteSource(source, startedIn);
        // A restarted PicView, and one whose Source the message cannot carry, start normally and still listen
        var handsOver = !IsOwnProcess(args) && HandOverMessage.CanCarry(source);
        var starting = new HandOverListener(pipeName, lockFile);
        var kind = Decide(
            patient => handsOver && HandOverSender.TrySendAsync(pipeName, source,
                    patient ? patientConnectTimeout : TimeSpan.Zero, acknowledgeTimeout, connected)
                .GetAwaiter().GetResult(),
            starting.TryStart);
        if (kind is AppStartKind.HandedOver)
        {
            return true;
        }

        listener = starting;
        if (kind is AppStartKind.Alone)
        {
            _ = starting.StartWhenFree(RetryListening, RetryListeningAttempts);
        }

        return false;
    }

    /// <summary>
    /// Decides how this process starts. A hand-over is tried once without waiting; when nothing takes it, this PicView
    /// tries to listen. When it cannot, because another PicView began listening in between or one is still exiting,
    /// the hand-over is tried once more, patiently, and only then does this PicView start alone.
    /// </summary>
    /// <param name="handOver">Hands the Source over, patiently or not, and says whether it was taken.</param>
    /// <param name="listen">Starts listening, and says whether this PicView is now the one that listens.</param>
    internal static AppStartKind Decide(Func<bool, bool> handOver, Func<bool> listen)
    {
        if (handOver(false))
        {
            return AppStartKind.HandedOver;
        }

        if (listen())
        {
            return AppStartKind.Listening;
        }

        return handOver(true) ? AppStartKind.HandedOver : AppStartKind.Alone;
    }

    /// <summary>
    /// The Source PicView was started with, or null for none.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="first">Where the arguments begin: 0 as <c>Main</c> gets them, 1 when the program comes first, as
    /// from <see cref="Environment.GetCommandLineArgs"/>.</param>
    public static string? SourceOf(IReadOnlyList<string> args, int first = 0)
    {
        for (var i = first; i < args.Count; i++)
        {
            if (!IsOwnProcessArgument(args[i]))
            {
                return string.IsNullOrWhiteSpace(args[i]) ? null : args[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Whether this PicView was started to run its own App Start, as by a restart.
    /// </summary>
    internal static bool IsOwnProcess(IReadOnlyList<string> args) => args.Count > 0 && IsOwnProcessArgument(args[0]);

    private static bool IsOwnProcessArgument(string arg) =>
        string.Equals(arg, OwnProcessArgument, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a start with this argument stays out of it: the elevated process that sets file associations does its
    /// work and exits, and has nothing to show.
    /// </summary>
    internal static bool IsExempt(string? source) =>
        source?.StartsWith("associate:", StringComparison.OrdinalIgnoreCase) == true;
}
