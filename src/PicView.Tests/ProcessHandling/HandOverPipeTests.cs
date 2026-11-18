using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using PicView.Core.ProcessHandling;

namespace PicView.Tests.ProcessHandling;

/// <summary>
/// A hand-over between two PicView processes, over a real pipe with a name of its own, inside one process.
/// </summary>
public class HandOverPipeTests : IDisposable
{
    private static readonly TimeSpan Connect = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Acknowledge = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    // Long enough for a listener to open a Source it should not, had it been going to
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(400);

    private readonly string _pipe = "PicViewTests-" + Guid.NewGuid().ToString("N");
    private readonly List<HandOverListener> _listeners = [];
    private readonly ConcurrentQueue<string?> _received = new();
    private readonly ConcurrentQueue<string?> _startedAlone = new();
    private readonly List<string> _lockFiles = [];

    public void Dispose()
    {
        foreach (var listener in _listeners)
        {
            listener.Stop();
            listener.ReleaseLock();
        }

        foreach (var file in _lockFiles)
        {
            File.Delete(file);
        }
    }

    private HandOverListener NewListener(string? lockFile = null, string? pipe = null)
    {
        var listener = new HandOverListener(pipe ?? _pipe, lockFile, _startedAlone.Enqueue);
        _listeners.Add(listener);
        return listener;
    }

    // A listener that takes every Source and records it
    private HandOverListener NewReceivingListener()
    {
        var listener = NewListener();
        listener.Attach(source =>
        {
            _received.Enqueue(source);
            return true;
        });
        Assert.True(listener.TryStart());
        return listener;
    }

    private string NewLockFile()
    {
        var file = Path.Combine(Path.GetTempPath(), "PicViewTests-" + Guid.NewGuid().ToString("N") + ".lock");
        _lockFiles.Add(file);
        return file;
    }

    private Task<bool> SendAsync(string? source) => HandOverSender.TrySendAsync(_pipe, source, Connect, Acknowledge);

    // The listener gets a Source once the PicView that sent it has confirmed, a moment after that one was answered
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < WaitLimit)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        Assert.True(condition(), "What was waited for did not happen in time");
    }

    [Fact]
    public async Task TrySend_NoPicViewListening_FailsWithoutWaiting()
    {
        var watch = Stopwatch.StartNew();

        var sent = await HandOverSender.TrySendAsync(_pipe, @"C:\a.jpg", TimeSpan.Zero, Acknowledge);

        Assert.False(sent);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"Took {watch.Elapsed}");
    }

    [Fact]
    public async Task TrySend_ToAListeningPicView_DeliversTheSource()
    {
        NewReceivingListener();

        Assert.True(await SendAsync(@"C:\pictures\a b\ø.jpg"));

        await WaitUntilAsync(() => _received.Count == 1);
        Assert.Equal([@"C:\pictures\a b\ø.jpg"], _received);
        Assert.Empty(_startedAlone);
    }

    [Fact]
    public async Task TrySend_WithoutASource_DeliversNone()
    {
        NewReceivingListener();

        Assert.True(await SendAsync(null));

        await WaitUntilAsync(() => _received.Count == 1);
        Assert.Equal([null], _received);
    }

    [Fact]
    public async Task TrySend_ASourceWithALineBreak_IsNotHandedOver()
    {
        NewReceivingListener();

        Assert.False(await SendAsync("a\nb.jpg"));
        Assert.False(await SendAsync("a\rb.jpg"));

        await Task.Delay(Settle, TestContext.Current.CancellationToken);
        Assert.Empty(_received);
    }

    [Fact]
    public async Task TrySend_TheReceiverDoesNotTakeWhatWasConfirmed_GoesToANewPicView()
    {
        // The PicView that sent it has exited by then, so refusing would lose the Source
        var listener = NewListener();
        listener.Attach(_ => false);
        Assert.True(listener.TryStart());

        Assert.True(await SendAsync(@"C:\a.jpg"));

        await WaitUntilAsync(() => _startedAlone.Count == 1);
        Assert.Equal([@"C:\a.jpg"], _startedAlone);
    }

    [Fact]
    public async Task TrySend_SeveralInARow_DeliversEachInOrder()
    {
        NewReceivingListener();

        Assert.True(await SendAsync("1"));
        await WaitUntilAsync(() => _received.Count == 1);
        Assert.True(await SendAsync("2"));
        await WaitUntilAsync(() => _received.Count == 2);
        Assert.True(await SendAsync("3"));
        await WaitUntilAsync(() => _received.Count == 3);

        Assert.Equal(["1", "2", "3"], _received);
    }

    [Fact]
    public async Task TrySend_SeveralAtOnce_DeliversEveryOne()
    {
        NewReceivingListener();

        var sent = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(i => Task.Run(() => SendAsync(i.ToString()))));

        Assert.All(sent, Assert.True);
        await WaitUntilAsync(() => _received.Count == 8);
        Assert.Equal(Enumerable.Range(0, 8).Select(i => i.ToString()), _received.Order());
    }

    [Fact]
    public async Task TrySend_BeforeAppStartTakesHandOvers_IsHeldUntilItDoes()
    {
        var received = new List<string?>();
        var listener = NewListener();
        Assert.True(listener.TryStart());

        Assert.True(await SendAsync(@"C:\a.jpg"));
        await WaitUntilAsync(() => listener.Held == 1);
        Assert.True(await SendAsync(null));
        await WaitUntilAsync(() => listener.Held == 2);
        listener.Attach(source =>
        {
            received.Add(source);
            return true;
        });

        Assert.Equal([@"C:\a.jpg", null], received);
        Assert.Equal(0, listener.Held);
    }

    [Fact]
    public async Task Stop_WithSourcesHeldForAnAppStartThatNeverTookThem_ReturnsThem()
    {
        var listener = NewListener();
        Assert.True(listener.TryStart());
        Assert.True(await SendAsync(@"C:\a.jpg"));
        await WaitUntilAsync(() => listener.Held == 1);

        var held = listener.Stop();

        Assert.Equal([@"C:\a.jpg"], held);
        Assert.Empty(listener.Stop());
    }

    [Fact]
    public async Task TrySend_AfterTheListenerStopped_Fails()
    {
        var listener = NewReceivingListener();
        Assert.True(await SendAsync(null));
        await WaitUntilAsync(() => _received.Count == 1);

        listener.Stop();

        Assert.False(listener.IsListening);
        Assert.False(await HandOverSender.TrySendAsync(_pipe, null, TimeSpan.FromMilliseconds(200), Acknowledge));
        Assert.Single(_received);
    }

    [Fact]
    public async Task TrySend_ThePicViewThereNeverAnswers_GivesUpAfterTheAcknowledgeTime()
    {
        // As a PicView that is still exiting: something holds the pipe, and nothing reads it
        var silent = new NamedPipeServerStream(_pipe, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await using (silent.ConfigureAwait(true))
        {
            var connected = silent.WaitForConnectionAsync(TestContext.Current.CancellationToken);
            var watch = Stopwatch.StartNew();

            var sent = await HandOverSender.TrySendAsync(_pipe, @"C:\a.jpg", Connect, TimeSpan.FromMilliseconds(300));

            Assert.False(sent);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"Took {watch.Elapsed}");
            await connected;
        }
    }

    [Fact]
    public async Task Listener_ThePicViewThatSentGaveUpBeforeItWasAnswered_OpensNothing()
    {
        // The PicView that gave up starts on its own and opens the Source itself, so here it must not open as well
        NewReceivingListener();
        var gaveUp = new NamedPipeClientStream(".", _pipe, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await using (gaveUp.ConfigureAwait(true))
        {
            await gaveUp.ConnectAsync(2000, TestContext.Current.CancellationToken);
            await gaveUp.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Write(@"C:\a.jpg") + "\n"),
                TestContext.Current.CancellationToken);
            // Closed without reading the answer, and so without confirming it
        }

        await Task.Delay(Settle, TestContext.Current.CancellationToken);
        Assert.Empty(_received);
        Assert.Empty(_startedAlone);
        // And the listener goes on taking hand-overs
        Assert.True(await SendAsync(@"C:\b.jpg"));
        await WaitUntilAsync(() => _received.Count == 1);
        Assert.Equal([@"C:\b.jpg"], _received);
    }

    [Fact]
    public async Task Listener_ThePicViewThatSentReadTheAnswerAndLeftWithoutConfirming_OpensNothing()
    {
        NewReceivingListener();
        var left = new NamedPipeClientStream(".", _pipe, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        string? answer;
        await using (left.ConfigureAwait(true))
        {
            await left.ConnectAsync(2000, TestContext.Current.CancellationToken);
            await left.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Write(@"C:\a.jpg") + "\n"),
                TestContext.Current.CancellationToken);
            using var reader = new StreamReader(left, Encoding.UTF8, false, 256, leaveOpen: true);
            answer = await reader.ReadLineAsync(TestContext.Current.CancellationToken);
        }

        await Task.Delay(Settle, TestContext.Current.CancellationToken);
        Assert.Equal(HandOverMessage.Ready, answer);
        Assert.Empty(_received);
        Assert.Empty(_startedAlone);
    }

    [Fact]
    public async Task Listener_ThePicViewThatSentConfirmsLate_StillOpensTheSource()
    {
        NewReceivingListener();
        var slow = new NamedPipeClientStream(".", _pipe, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await using (slow.ConfigureAwait(true))
        {
            await slow.ConnectAsync(2000, TestContext.Current.CancellationToken);
            await slow.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Write(@"C:\a.jpg") + "\n"),
                TestContext.Current.CancellationToken);
            using var reader = new StreamReader(slow, Encoding.UTF8, false, 256, leaveOpen: true);
            Assert.Equal(HandOverMessage.Ready, await reader.ReadLineAsync(TestContext.Current.CancellationToken));

            // Longer than the listener gives the first line and its answer
            await Task.Delay(TimeSpan.FromSeconds(2.5), TestContext.Current.CancellationToken);
            await slow.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Confirmed + "\n"),
                TestContext.Current.CancellationToken);
        }

        await WaitUntilAsync(() => _received.Count == 1);
        Assert.Equal([@"C:\a.jpg"], _received);
    }

    [Fact]
    public async Task Listener_StoppedBetweenTheAnswerAndTheConfirmation_GivesTheSourceToANewPicView()
    {
        var listener = NewReceivingListener();
        var sender = new NamedPipeClientStream(".", _pipe, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await using (sender.ConfigureAwait(true))
        {
            await sender.ConnectAsync(2000, TestContext.Current.CancellationToken);
            await sender.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Write(@"C:\a.jpg") + "\n"),
                TestContext.Current.CancellationToken);
            using var reader = new StreamReader(sender, Encoding.UTF8, false, 256, leaveOpen: true);
            Assert.Equal(HandOverMessage.Ready, await reader.ReadLineAsync(TestContext.Current.CancellationToken));

            // App Shutdown begins here, after this PicView was told the running one is ready
            listener.Stop();
            await sender.WriteAsync(Encoding.UTF8.GetBytes(HandOverMessage.Confirmed + "\n"),
                TestContext.Current.CancellationToken);
        }

        await WaitUntilAsync(() => _startedAlone.Count == 1);
        Assert.Equal([@"C:\a.jpg"], _startedAlone);
        Assert.Empty(_received);
    }

    [Fact]
    public void TryStart_WhileAnotherPicViewListens_Fails()
    {
        // The pipe is one PicView's only on Windows; elsewhere the lock file decides it
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Only Windows refuses a second listener on the pipe itself");
        Assert.True(NewListener().TryStart());

        var second = NewListener();

        Assert.False(second.TryStart());
        Assert.False(second.IsListening);
    }

    [Fact]
    public async Task StartWhenFree_OnceTheOtherPicViewStops_Listens()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Only Windows refuses a second listener on the pipe itself");
        var first = NewListener();
        Assert.True(first.TryStart());
        var second = NewListener();
        second.Attach(source =>
        {
            _received.Enqueue(source);
            return true;
        });
        Assert.False(second.TryStart());

        var listening = second.StartWhenFree(TimeSpan.FromMilliseconds(20), attempts: 500);
        first.Stop();

        Assert.True(await listening);
        Assert.True(await SendAsync("after"));
        await WaitUntilAsync(() => _received.Count == 1);
        Assert.Equal(["after"], _received);
    }

    [Fact]
    public void TryStart_AfterStop_DoesNotListenAgain()
    {
        var listener = NewListener();
        Assert.True(listener.TryStart());

        listener.Stop();

        Assert.False(listener.TryStart());
    }

    [Fact]
    public void TryStart_WithALockFileAnotherPicViewHolds_DoesNotListenEvenOnAPipeThatIsFree()
    {
        // Where the pipe is a socket file, a second listener would take the first one's file away, so the lock decides.
        // The two have pipes of their own here, so only the lock can be what stops the second
        var lockFile = NewLockFile();
        var first = NewListener(lockFile, _pipe + "-1");
        var second = NewListener(lockFile, _pipe + "-2");

        Assert.True(first.TryStart());

        Assert.False(second.TryStart());
        Assert.False(second.IsListening);
    }

    [Fact]
    public void TryStart_WithALockFileOfAPicViewThatStopped_StillDoesNotListenUntilThatProcessIsGone()
    {
        // A stopped PicView closes its socket file a moment later, which would take a new listener's with it
        var lockFile = NewLockFile();
        var first = NewListener(lockFile, _pipe + "-1");
        var second = NewListener(lockFile, _pipe + "-2");
        Assert.True(first.TryStart());

        first.Stop();
        var whileTheProcessLives = second.TryStart();
        // What the end of the first PicView's process does
        first.ReleaseLock();
        var afterItIsGone = second.TryStart();

        Assert.False(whileTheProcessLives);
        Assert.True(afterItIsGone);
    }

    [Fact]
    public void TryStart_WithALockFileNoPicViewHolds_ListensAndLeavesTheFileBehindHarmlessly()
    {
        // A crashed PicView leaves the file, unlocked
        var lockFile = NewLockFile();
        File.WriteAllText(lockFile, "");

        Assert.True(NewListener(lockFile).TryStart());
    }
}
