using PicView.Core.ProcessHandling;

namespace PicView.Tests.ProcessHandling;

public class SingleProcessTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "PicViewSingleProcessTests_" + Guid.NewGuid().ToString("N"));

    public SingleProcessTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void Decide_ARunningPicViewTakesTheHandOver_HandsOverWithoutListening()
    {
        var listens = 0;

        var kind = SingleProcess.Decide(_ => true, () =>
        {
            listens++;
            return true;
        });

        Assert.Equal(AppStartKind.HandedOver, kind);
        Assert.Equal(0, listens);
    }

    [Fact]
    public void Decide_NoPicViewRunning_StartsAndListensAfterOneQuickAttempt()
    {
        List<bool> patience = [];

        var kind = SingleProcess.Decide(patient =>
        {
            patience.Add(patient);
            return false;
        }, () => true);

        Assert.Equal(AppStartKind.Listening, kind);
        Assert.Equal([false], patience);
    }

    [Fact]
    public void Decide_AnotherPicViewBeganListeningMeanwhile_HandsOverPatiently()
    {
        List<bool> patience = [];

        var kind = SingleProcess.Decide(patient =>
        {
            patience.Add(patient);
            return patient;
        }, () => false);

        Assert.Equal(AppStartKind.HandedOver, kind);
        Assert.Equal([false, true], patience);
    }

    [Fact]
    public void Decide_TheListeningPicViewNeverTakesIt_StartsAlone()
    {
        var kind = SingleProcess.Decide(_ => false, () => false);

        Assert.Equal(AppStartKind.Alone, kind);
    }

    [Theory]
    [InlineData("associate:.jpg,.png", true)]
    [InlineData("ASSOCIATE:.jpg", true)]
    [InlineData(@"C:\pictures\a.jpg", false)]
    [InlineData(null, false)]
    public void IsExempt_OnlyAFileAssociationStart(string? source, bool expected) =>
        Assert.Equal(expected, SingleProcess.IsExempt(source));

    [Theory]
    // App Shutdown has begun: the new PicView starts on its own, whatever else holds
    [InlineData(true, false, true, true, HandOverResponse.Refuse)]
    [InlineData(true, true, true, true, HandOverResponse.Refuse)]
    [InlineData(true, false, false, false, HandOverResponse.Refuse)]
    // Open in same window is off: always a new Main Window
    [InlineData(false, false, true, true, HandOverResponse.OpenInNewMainWindow)]
    [InlineData(false, false, false, true, HandOverResponse.OpenInNewMainWindow)]
    // Open in same window is on
    [InlineData(false, true, true, true, HandOverResponse.OpenInActiveMainWindow)]
    [InlineData(false, true, false, true, HandOverResponse.ActivateActiveMainWindow)]
    // No Main Window is open, as while only Batch Resize is: there is none to open in or bring forward
    [InlineData(false, true, true, false, HandOverResponse.OpenInNewMainWindow)]
    [InlineData(false, true, false, false, HandOverResponse.OpenInNewMainWindow)]
    [InlineData(false, false, true, false, HandOverResponse.OpenInNewMainWindow)]
    public void Respond_FollowsShutdownThenTheSettingThenTheSource(bool isShuttingDown, bool openInSameWindow,
        bool hasSource, bool hasMainWindow, HandOverResponse expected) =>
        Assert.Equal(expected, HandOverReceipt.Respond(isShuttingDown, openInSameWindow, hasSource, hasMainWindow));

    [Theory]
    [InlineData(@"C:\pictures\a b\ø.jpg")]
    [InlineData("https://example.com/a.png?x=1&y=2")]
    [InlineData("new")]
    [InlineData("")]
    public void Message_WithASource_ReadsBackTheSameSource(string source)
    {
        Assert.True(HandOverMessage.TryRead(HandOverMessage.Write(source), out var read));
        Assert.Equal(source, read);
    }

    [Fact]
    public void Message_WithoutASource_ReadsBackNone()
    {
        Assert.True(HandOverMessage.TryRead(HandOverMessage.Write(null), out var read));
        Assert.Null(read);
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\pictures\a.jpg")]
    [InlineData("something else")]
    public void Message_NotAHandOver_IsNotRead(string line) =>
        Assert.False(HandOverMessage.TryRead(line, out _));

    [Fact]
    public void AbsoluteSource_RelativeFile_IsResolvedAgainstTheStartingFolder()
    {
        File.WriteAllText(Path.Combine(_folder, "a.jpg"), "image");

        Assert.Equal(Path.Combine(_folder, "a.jpg"), HandOverMessage.AbsoluteSource("a.jpg", _folder));
    }

    [Fact]
    public void AbsoluteSource_RelativeFolder_IsResolvedAgainstTheStartingFolder()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "sub"));

        Assert.Equal(Path.Combine(_folder, "sub"), HandOverMessage.AbsoluteSource("sub", _folder));
    }

    [Theory]
    [InlineData("https://example.com/a.png")]
    [InlineData("missing.jpg")]
    [InlineData(null)]
    public void AbsoluteSource_NotAFileOrFolderThere_IsLeftAsItIs(string? source) =>
        Assert.Equal(source, HandOverMessage.AbsoluteSource(source, _folder));

    [Fact]
    public void PipeName_IsThisUsersOwn()
    {
        Assert.StartsWith("PicView-HandOver-" + Environment.UserName, HandOverPipe.Name);
        Assert.StartsWith(HandOverPipe.Name, Path.GetFileName(HandOverPipe.LockFile));
    }

    [Theory]
    [InlineData("a.jpg", true)]
    [InlineData(null, true)]
    [InlineData("a\nb.jpg", false)]
    [InlineData("a\rb.jpg", false)]
    public void CanCarry_OnlyASourceOnOneLine(string? source, bool expected) =>
        Assert.Equal(expected, HandOverMessage.CanCarry(source));

    [Fact]
    public void SourceOf_AsMainGetsThem_IsTheFirstArgument() =>
        Assert.Equal(@"C:\a.jpg", SingleProcess.SourceOf([@"C:\a.jpg", "more"]));

    [Fact]
    public void SourceOf_WithTheProgramFirst_IsTheArgumentAfterIt() =>
        Assert.Equal(@"C:\a.jpg", SingleProcess.SourceOf([@"C:\PicView.exe", @"C:\a.jpg"], first: 1));

    [Fact]
    public void SourceOf_StartedAsItsOwnProcess_IsNotThatArgument()
    {
        Assert.Null(SingleProcess.SourceOf([SingleProcess.OwnProcessArgument]));
        Assert.Equal(@"C:\a.jpg", SingleProcess.SourceOf([SingleProcess.OwnProcessArgument, @"C:\a.jpg"]));
        Assert.Null(SingleProcess.SourceOf([@"C:\PicView.exe", SingleProcess.OwnProcessArgument], first: 1));
    }

    [Fact]
    public void SourceOf_WithNothingToOpen_IsNone()
    {
        Assert.Null(SingleProcess.SourceOf([]));
        Assert.Null(SingleProcess.SourceOf([" "]));
    }

    [Theory]
    [InlineData(@"C:\pictures\a b.jpg")]
    [InlineData(@"\\server\share\a.jpg")]
    [InlineData(@"C:\pictures\folder\")]
    [InlineData(@"C:\a ""quoted"" name.jpg")]
    [InlineData("g")]
    public void StartInfoFor_ASource_PassesItAsOneArgumentUnchanged(string source)
    {
        var info = ProcessHelper.StartInfoFor(@"C:\PicView.exe", source, ownProcess: false);

        Assert.Equal(@"C:\PicView.exe", info.FileName);
        Assert.Equal([source], info.ArgumentList);
        Assert.Empty(info.Arguments);
    }

    [Fact]
    public void StartInfoFor_ARestart_SaysItIsItsOwnProcessBeforeAnySource()
    {
        Assert.Equal([SingleProcess.OwnProcessArgument],
            ProcessHelper.StartInfoFor(@"C:\PicView.exe", null, ownProcess: true).ArgumentList);
        Assert.Equal([SingleProcess.OwnProcessArgument, "a.jpg"],
            ProcessHelper.StartInfoFor(@"C:\PicView.exe", "a.jpg", ownProcess: true).ArgumentList);
        Assert.Empty(ProcessHelper.StartInfoFor(@"C:\PicView.exe", null, ownProcess: false).ArgumentList);
    }
}

/// <summary>
/// The start of a PicView process, as <c>Main</c> runs it, over a real pipe with a name of its own.
/// </summary>
public class SingleProcessStartTests : IDisposable
{
    private static readonly TimeSpan Patient = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Acknowledge = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly string _pipe = "PicViewTests-" + Guid.NewGuid().ToString("N");
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "PicViewSingleProcessStartTests_" + Guid.NewGuid().ToString("N"));
    private readonly List<HandOverListener> _listeners = [];
    private readonly System.Collections.Concurrent.ConcurrentQueue<string?> _received = new();

    public SingleProcessStartTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        foreach (var listener in _listeners)
        {
            listener.Stop();
        }

        Directory.Delete(_folder, true);
    }

    // What Main does, with this test's pipe, folder and short waits
    private bool Start(string[] args, out HandOverListener? listener)
    {
        var handedOver = SingleProcess.HandOverAtStart(args, _pipe, null, _folder, Patient, Acknowledge, null,
            out listener);
        if (listener is not null)
        {
            _listeners.Add(listener);
        }

        return handedOver;
    }

    private void RunPicView()
    {
        var running = new HandOverListener(_pipe);
        _listeners.Add(running);
        running.Attach(source =>
        {
            _received.Enqueue(source);
            return true;
        });
        Assert.True(running.TryStart());
    }

    private async Task WaitUntilReceivedAsync(int count)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (_received.Count < count && watch.Elapsed < WaitLimit)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        Assert.Equal(count, _received.Count);
    }

    [Fact]
    public void HandOverAtStart_NoPicViewRunning_StartsAndListens()
    {
        var handedOver = Start([@"C:\a.jpg"], out var listener);

        Assert.False(handedOver);
        Assert.NotNull(listener);
        Assert.True(listener.IsListening);
    }

    [Fact]
    public async Task HandOverAtStart_APicViewRunning_HandsOverTheFullPathOfItsFirstArgument()
    {
        RunPicView();
        File.WriteAllText(Path.Combine(_folder, "a.jpg"), "image");

        // The arguments as Main gets them: the Source first, not the program
        var handedOver = Start(["a.jpg"], out var listener);

        Assert.True(handedOver);
        Assert.Null(listener);
        await WaitUntilReceivedAsync(1);
        Assert.Equal([Path.Combine(_folder, "a.jpg")], _received);
    }

    [Fact]
    public async Task HandOverAtStart_APicViewRunningAndNoArguments_HandsOverWithoutASource()
    {
        RunPicView();

        Assert.True(Start([], out _));

        await WaitUntilReceivedAsync(1);
        Assert.Equal([null], _received);
    }

    [Fact]
    public async Task HandOverAtStart_AFileAssociationStart_NeitherHandsOverNorListens()
    {
        RunPicView();

        var handedOver = Start(["associate:.jpg,.png"], out var listener);

        Assert.False(handedOver);
        Assert.Null(listener);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Empty(_received);
    }

    [Fact]
    public async Task HandOverAtStart_StartedAsItsOwnProcess_HandsNothingOverToThePicViewThatListens()
    {
        // As the PicView a restart starts while another PicView is the one that listens
        RunPicView();

        var handedOver = Start([SingleProcess.OwnProcessArgument], out var listener);

        Assert.False(handedOver);
        Assert.NotNull(listener);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Empty(_received);
    }

    [Fact]
    public void HandOverAtStart_StartedAsItsOwnProcessWithNoPicViewRunning_Listens()
    {
        var handedOver = Start([SingleProcess.OwnProcessArgument], out var listener);

        Assert.False(handedOver);
        Assert.True(listener!.IsListening);
    }

    [Fact]
    public async Task HandOverAtStart_ASourceWithALineBreak_StartsNormallyInsteadOfHandingOver()
    {
        RunPicView();

        var handedOver = Start(["a\nb.jpg"], out var listener);

        Assert.False(handedOver);
        Assert.NotNull(listener);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Empty(_received);
    }

    [Fact]
    public async Task HandOverAtStart_ThePipeIsHeldByAPicViewThatNeverAnswers_StartsAloneWithinTheWaits()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Only Windows refuses a second listener on the pipe itself");
        // As a PicView that is still exiting
        var silent = new System.IO.Pipes.NamedPipeServerStream(_pipe, System.IO.Pipes.PipeDirection.InOut, 1,
            System.IO.Pipes.PipeTransmissionMode.Byte,
            System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
        await using (silent.ConfigureAwait(true))
        {
            var connected = silent.WaitForConnectionAsync(TestContext.Current.CancellationToken);
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var handedOver = Start([@"C:\a.jpg"], out var listener);

            Assert.False(handedOver);
            Assert.NotNull(listener);
            Assert.False(listener.IsListening);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4), $"Took {watch.Elapsed}");
            await connected;
        }
    }
}
