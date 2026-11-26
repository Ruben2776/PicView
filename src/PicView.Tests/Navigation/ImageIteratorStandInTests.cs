using System.Collections.Concurrent;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Thumbnailing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// What a tab shows while the image it goes to still loads: its thumbnail as a Stand-in, unless the image is there
/// first.
/// </summary>
[Collection("Sequential")]
public class ImageIteratorStandInTests : IDisposable
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "PicViewIteratorStandInTests");

    private readonly ManualResetEventSlim _decodeGate = new();
    private readonly GatedThumbnails _thumbnails = new();
    private readonly ConcurrentQueue<object?> _shownImages = new();
    private readonly FileInfo[] _files = [File("a.jpg"), File("b.jpg")];
    private readonly CancellationTokenSource _stepCancellation = new();
    private readonly TabViewModel _tab;
    private readonly ImageIterator _iterator;

    public ImageIteratorStandInTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();

        var cache = new SharedImageCache(f =>
        {
            _decodeGate.Wait(WaitLimit);
            return new ImageModel { FileInfo = f, Image = new object() };
        });
        _tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!),
            TestTabServices.For(cache, new Thumbnails(_thumbnails, cache)));
        _tab.Initialize();
        _iterator = (ImageIterator)_tab.ImageIterator!;
        _iterator.Initialize(_files, 0);
        _tab.Image.Subscribe(_shownImages.Enqueue);
    }

    public void Dispose()
    {
        _decodeGate.Set();
        _thumbnails.Gate.Set();
        _tab.Dispose();
        _stepCancellation.Dispose();
    }

    [Fact]
    public async Task IterateToIndex_ImageLoadsBeforeItsThumbnail_ShowsItWithoutWaiting()
    {
        var step = _iterator.IterateToIndexAsync(1, _stepCancellation).AsTask();

        _decodeGate.Set();
        await WaitUntil(() => IsShown(_files[1]), "The loaded image waited for its thumbnail");

        _thumbnails.Gate.Set();
        await step;
    }

    [Fact]
    public async Task IterateToIndex_ThumbnailComesFirst_ShowsItAsTheStandIn()
    {
        _thumbnails.Gate.Set();
        var step = _iterator.IterateToIndexAsync(1, _stepCancellation).AsTask();

        await WaitUntil(() => _shownImages.Contains(_thumbnails.Thumbnail), "The thumbnail was not shown as the Stand-in");
        Assert.False(IsShown(_files[1]));

        _decodeGate.Set();
        await step;
        Assert.True(IsShown(_files[1]));
    }

    [Fact]
    public async Task IterateToIndex_FileHasNoThumbnail_ClearsWhatIsShownUntilTheImageLoads()
    {
        _tab.ShowStandIn(new object());
        _shownImages.Clear();
        _thumbnails.Thumbnail = null;
        _thumbnails.Gate.Set();
        var step = _iterator.IterateToIndexAsync(1, _stepCancellation).AsTask();

        await WaitUntil(() => _shownImages.Contains(null), "No thumbnail should clear the old image while loading");
        Assert.False(IsShown(_files[1]));

        _decodeGate.Set();
        await step;
        Assert.True(IsShown(_files[1]));
    }

    [Fact]
    public async Task IterateToIndex_ThumbnailFails_StillShowsTheImage()
    {
        _thumbnails.Failure = new InvalidOperationException("no thumbnail");
        _thumbnails.Gate.Set();
        var step = _iterator.IterateToIndexAsync(1, _stepCancellation).AsTask();

        _decodeGate.Set();
        await step;

        Assert.True(IsShown(_files[1]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IterateToIndex_ThumbnailFailsOrIsCancelledBeforeTheImageLoads_KeepsWhatIsShown(bool cancelled)
    {
        _tab.ShowStandIn(new object());
        _shownImages.Clear();
        _thumbnails.Failure = cancelled ? new OperationCanceledException() : new InvalidOperationException("no thumbnail");
        _thumbnails.Gate.Set();
        var step = _iterator.IterateToIndexAsync(1, _stepCancellation).AsTask();

        await _thumbnails.Answered.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        // Nothing tells when the step is past the failure, so this waits long enough for it to act on it, were it to.
        // On a machine too busy for that the test passes without guarding; it never fails wrongly
        await Task.Delay(300, TestContext.Current.CancellationToken);
        _decodeGate.Set();
        await step;

        Assert.DoesNotContain(null, _shownImages);
    }

    private bool IsShown(FileInfo file) =>
        string.Equals(_tab.Model.FileInfo?.FullName, file.FullName, StringComparison.Ordinal);

    private static FileInfo File(string name) => new(Path.Combine(TestDirectory, name));

    private static async Task WaitUntil(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, because);
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    // Gives one ready-made thumbnail once its gate opens, and decodes nothing
    private sealed class GatedThumbnails : IThumbnailDecoder
    {
        public ManualResetEventSlim Gate { get; } = new();
        // Null for a file that has none
        public object? Thumbnail { get; set; } = new();

        // Thrown in place of an answer
        public Exception? Failure { get; set; }

        // Set once the ready-made thumbnail was asked for and its gate let it answer
        public TaskCompletionSource Answered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public object? GetReadyMade(FileInfo file, uint height)
        {
            Gate.Wait(WaitLimit + WaitLimit);
            Answered.TrySetResult();
            return Failure is { } failure ? throw failure : Thumbnail;
        }

        public object? Decode(FileInfo file, uint height) => null;
        public object? DecodeWithoutReadyMade(FileInfo file, uint height) => null;
        public long SizeInBytes(object thumbnail) => 0;
        public object? ScaleDown(object image, uint height) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
