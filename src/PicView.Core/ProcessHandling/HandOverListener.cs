using System.IO.Pipes;
using System.Text;
using PicView.Core.DebugTools;

namespace PicView.Core.ProcessHandling;

/// <summary>
/// The running PicView's side of a hand-over: it listens on the pipe, answers each PicView that connects, and gives
/// the Source to the receiver once that PicView has confirmed it read the answer. A PicView that gave up waiting never
/// confirms, so its Source is not opened here as well as by itself. Hand-overs that arrive before a receiver is
/// attached, while App Start is still opening the first Main Window, are held for it. Once stopped it refuses, so a
/// PicView that started during App Shutdown starts on its own. A Source that was confirmed and then cannot be given
/// to the receiver, because the listener stopped in between or the receiver did not take it, goes to a new PicView
/// process, so it is never lost.
/// </summary>
/// <param name="pipeName">The pipe to listen on.</param>
/// <param name="lockFile">When given, only the process that holds this file locked may listen, and it holds it until
/// it exits. It is how one listener is decided where the pipe itself cannot decide it (see <see cref="TryCreate"/>).</param>
/// <param name="startAlone">Starts a new PicView process with a Source that was confirmed but can no longer be opened
/// here.</param>
public sealed class HandOverListener(string pipeName, string? lockFile = null, Action<string?>? startAlone = null)
{
    // How long a connected PicView gets to send its line, and this one to answer it
    private static readonly TimeSpan ExchangeTimeout = TimeSpan.FromSeconds(2);

    // How long it then gets to confirm: much longer than it waits for the answer, since once it was told this PicView
    // is ready, a confirmation that is late must still be read. One that never comes, from a PicView that died, ends
    // when its pipe closes, well before this
    private static readonly TimeSpan ConfirmTimeout = TimeSpan.FromSeconds(30);

    private readonly Action<string?> _startAlone = startAlone ?? (static source => ProcessHelper.StartNewProcess(source));
    private readonly Lock _lock = new();
    private readonly List<string?> _held = [];
    private readonly CancellationTokenSource _stopping = new();
    private Func<string?, bool>? _receive;
    private NamedPipeServerStream? _waiting;
    private FileStream? _lockHeld;
    private bool _stopped;

    /// <summary>
    /// Whether this PicView is the one hand-overs are sent to.
    /// </summary>
    public bool IsListening { get; private set; }

    /// <summary>
    /// How many Sources are held for a receiver that is not attached yet.
    /// </summary>
    internal int Held
    {
        get
        {
            lock (_lock)
            {
                return _held.Count;
            }
        }
    }

    /// <summary>
    /// Starts listening, unless another PicView already is or this listener was stopped.
    /// </summary>
    /// <returns>Whether this PicView listens now.</returns>
    public bool TryStart()
    {
        lock (_lock)
        {
            if (_stopped || IsListening)
            {
                return IsListening;
            }

            if (!TryLock())
            {
                return false;
            }

            var first = TryCreate(first: true);
            if (first is null)
            {
                return false;
            }

            _waiting = first;
            IsListening = true;
            _ = Task.Run(() => ListenAsync(first));
            return true;
        }
    }

    /// <summary>
    /// Keeps trying to start listening, for a PicView that started while another one held the pipe without taking its
    /// hand-over, as a PicView that is exiting does to the one that restarts it.
    /// </summary>
    /// <returns>Whether it listens, once it does or the attempts are used up.</returns>
    public async Task<bool> StartWhenFree(TimeSpan between, int attempts)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (TryStart())
            {
                return true;
            }

            try
            {
                await Task.Delay(between, _stopping.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Gives every hand-over to <paramref name="receive"/> from now on, starting with the ones held so far, in the
    /// order they arrived. It is called on a pool thread, or on the caller's for the held ones, and answers whether
    /// it took the Source, null being a start without one. A Source it does not take goes to a new PicView process.
    /// </summary>
    public void Attach(Func<string?, bool> receive)
    {
        string?[] held;
        lock (_lock)
        {
            _receive = receive;
            held = _held.ToArray();
            _held.Clear();
        }

        foreach (var source in held)
        {
            Give(receive, source);
        }
    }

    /// <summary>
    /// Stops listening for good and refuses what still arrives. The pipe is closed when this returns.
    /// </summary>
    /// <returns>The Sources that were held for a receiver that was never attached. The caller owes each of them a
    /// PicView to open in.</returns>
    public IReadOnlyList<string?> Stop()
    {
        NamedPipeServerStream? waiting;
        string?[] held;
        lock (_lock)
        {
            if (_stopped)
            {
                return [];
            }

            _stopped = true;
            IsListening = false;
            waiting = _waiting;
            _waiting = null;
            held = _held.ToArray();
            _held.Clear();
        }

        _stopping.Cancel();
        waiting?.Dispose();
        return held;
    }

    /// <summary>
    /// Lets go of the lock file. Only a test does this: a PicView holds it until its process exits, since closing its
    /// pipe can take the socket of a PicView that began listening in between with it.
    /// </summary>
    internal void ReleaseLock()
    {
        lock (_lock)
        {
            _lockHeld?.Dispose();
            _lockHeld = null;
        }
    }

    // Callers hold _lock
    private bool TryLock()
    {
        if (lockFile is null || _lockHeld is not null)
        {
            return true;
        }

        try
        {
            // Sharing with no one is an exclusive lock that the system lets go of when the process ends, however it ends
            _lockHeld = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HandOverListener), nameof(TryLock), e);
            return false;
        }
    }

    private NamedPipeServerStream? TryCreate(bool first)
    {
        var options = PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;
        if (first && OperatingSystem.IsWindows())
        {
            // Fails while any PicView has the pipe, so only one listens. Elsewhere the pipe is a socket file: making
            // one removes the file that is there, a listening PicView's included, and a crashed PicView leaves its
            // file behind, so there the lock file decides who listens
            options |= PipeOptions.FirstPipeInstance;
        }

        try
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, options);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HandOverListener), nameof(TryCreate), e);
            return null;
        }
    }

    private async Task ListenAsync(NamedPipeServerStream pipe)
    {
        try
        {
            while (true)
            {
                await pipe.WaitForConnectionAsync(_stopping.Token).ConfigureAwait(false);
                var connected = pipe;
                NamedPipeServerStream? next;
                lock (_lock)
                {
                    // The next one before this one is answered, so the pipe is there for as long as PicView listens
                    next = _stopped ? null : TryCreate(first: false);
                    _waiting = next;
                    if (next is null)
                    {
                        IsListening = false;
                    }
                }

                _ = Task.Run(() => AnswerAsync(connected));
                if (next is null)
                {
                    return;
                }

                pipe = next;
            }
        }
        catch (Exception e)
        {
            if (e is not (OperationCanceledException or ObjectDisposedException))
            {
                DebugHelper.LogDebug(nameof(HandOverListener), nameof(ListenAsync), e);
            }

            await pipe.DisposeAsync().ConfigureAwait(false);
            lock (_lock)
            {
                if (ReferenceEquals(_waiting, pipe))
                {
                    _waiting = null;
                    IsListening = false;
                }
            }
        }
    }

    private async Task AnswerAsync(NamedPipeServerStream pipe)
    {
        await using (pipe.ConfigureAwait(false))
        {
            try
            {
                using var timeout = new CancellationTokenSource(ExchangeTimeout);
                using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
                var line = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
                var ready = HandOverMessage.TryRead(line, out var source) && !IsStopped();
                var answer = Encoding.UTF8.GetBytes((ready ? HandOverMessage.Ready : HandOverMessage.Refused) + "\n");
                await pipe.WriteAsync(answer, timeout.Token).ConfigureAwait(false);
                await pipe.FlushAsync(timeout.Token).ConfigureAwait(false);
                // The other PicView confirms once it has read the answer, and exits. One that gave up waiting has
                // closed the pipe instead and starts on its own, so its Source must not open here too. Reading also
                // waits for it to close, as closing first drops an answer it has not read yet
                using var confirming = new CancellationTokenSource(ConfirmTimeout);
                var confirmed = await reader.ReadLineAsync(confirming.Token).ConfigureAwait(false);
                if (ready && string.Equals(confirmed, HandOverMessage.Confirmed, StringComparison.Ordinal))
                {
                    Commit(source);
                }
            }
            catch (Exception e)
            {
                if (e is not (OperationCanceledException or IOException))
                {
                    DebugHelper.LogDebug(nameof(HandOverListener), nameof(AnswerAsync), e);
                }
            }
        }
    }

    private bool IsStopped()
    {
        lock (_lock)
        {
            return _stopped;
        }
    }

    // The other PicView has exited by now, so from here the Source is this PicView's to find a place for
    private void Commit(string? source)
    {
        Func<string?, bool>? receive;
        lock (_lock)
        {
            if (_stopped)
            {
                receive = null;
            }
            else if (_receive is null)
            {
                _held.Add(source);
                return;
            }
            else
            {
                receive = _receive;
            }
        }

        if (receive is null)
        {
            StartAlone(source);
            return;
        }

        Give(receive, source);
    }

    private void Give(Func<string?, bool> receive, string? source)
    {
        bool taken;
        try
        {
            taken = receive(source);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HandOverListener), nameof(Give), e);
            taken = false;
        }

        if (!taken)
        {
            StartAlone(source);
        }
    }

    private void StartAlone(string? source)
    {
        try
        {
            _startAlone(source);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HandOverListener), nameof(StartAlone), e);
        }
    }
}
