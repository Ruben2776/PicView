using System.Collections.Concurrent;
using System.Diagnostics;
using PicView.Core.Models;
using PicView.Core.Navigation;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class SharedImageCacheTests
{
    private const uint Owner = 1;
    private const uint OtherOwner = 2;

    private readonly ConcurrentDictionary<string, int> _loadCounts = new();
    private readonly SharedImageCache _cache;

    public SharedImageCacheTests()
    {
        SetDefaults();
        _cache = new SharedImageCache(LoadProbe);
    }

    [Fact]
    public async Task GetAsync_FileNotCached_LoadsThatFile()
    {
        var files = Files(3);
        _cache.SetFiles(Owner, files);

        var model = await _cache.GetAsync(Owner, files[1], TestContext.Current.CancellationToken);

        Assert.NotNull(model);
        Assert.Equal(files[1].FullName, model.FileInfo!.FullName);
    }

    [Fact]
    public async Task GetAsync_ImageReady_CompletesSynchronously()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        var again = _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        Assert.True(again.IsCompletedSuccessfully, "A cached image should be returned without awaiting");
        Assert.Same(model, again.Result);
    }

    [Fact]
    public async Task GetAsync_NotCached_DoesNotRunTheLoadOnTheCallersThread()
    {
        using var gate = new ManualResetEventSlim();
        // A loader that blocks synchronously: run on the caller's thread, it would hold up GetAsync itself
        var cache = new SharedImageCache(f =>
        {
            gate.Wait(TimeSpan.FromSeconds(5));
            return new ImageModel { FileInfo = f };
        });
        var files = Files(1);
        cache.SetFiles(Owner, files);

        var stopwatch = Stopwatch.StartNew();
        var request = cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        stopwatch.Stop();
        gate.Set();
        await request;

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), "GetAsync waited for a load run on the caller's thread");
    }

    [Fact]
    public async Task GetAsync_ConcurrentRequestsForSameFile_LoadOnce()
    {
        var gate = new TaskCompletionSource();
        var loads = 0;
        var cache = new SharedImageCache(f =>
        {
            Interlocked.Increment(ref loads);
            gate.Task.Wait(TestContext.Current.CancellationToken);
            return new ImageModel { FileInfo = f };
        });
        var files = Files(1);
        cache.SetFiles(Owner, files);
        cache.SetFiles(OtherOwner, files);

        var first = cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        var second = cache.GetAsync(OtherOwner, files[0], TestContext.Current.CancellationToken);
        gate.SetResult();

        Assert.Same(await first, await second);
        Assert.Equal(1, loads);
    }

    [Fact]
    public async Task GetAsync_Cancelled_ReturnsNullAndLoadStillCompletes()
    {
        var gate = new TaskCompletionSource();
        var cache = new SharedImageCache(f =>
        {
            gate.Task.Wait(TestContext.Current.CancellationToken);
            return new ImageModel { FileInfo = f, Image = new ProbeImage() };
        });
        var files = Files(1);
        cache.SetFiles(Owner, files);
        using var cts = new CancellationTokenSource();

        var request = cache.GetAsync(Owner, files[0], cts.Token);
        Assert.False(cache.TryGetReady(files[0], out _), "A loading image is not ready");
        await cts.CancelAsync();
        Assert.Null(await request);

        gate.SetResult();
        var model = await cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        Assert.True(cache.TryGetReady(files[0], out var ready));
        Assert.Same(model, ready);
    }

    [Fact]
    public async Task GetAsync_LoaderThrows_ReturnsModelWithoutImage()
    {
        var cache = new SharedImageCache(static _ => throw new InvalidOperationException("decode failed"));
        var files = Files(1);
        cache.SetFiles(Owner, files);

        var model = await cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        Assert.NotNull(model);
        Assert.Null(model.Image);
        Assert.Equal(files[0].FullName, model.FileInfo!.FullName);
    }

    [Fact]
    public async Task GetAsync_FileOutsideOwnerList_LoadsButIsNotHeld()
    {
        _cache.SetFiles(Owner, Files(2));
        var outsider = new FileInfo("outside.jpg");

        var model = await _cache.GetAsync(Owner, outsider, TestContext.Current.CancellationToken);
        _cache.ForceDisposalQueue();

        Assert.NotNull(model);
        Assert.True(Probe(model).IsDisposed, "No owner references a file outside every list");
    }

    [Fact]
    public void TryUseReady_ImageReleasedDuringUse_IsNotDisposedUntilTheUseEnds()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = new ImageModel { FileInfo = files[0], Image = new ProbeImage() };
        _cache.Put(Owner, files[0], model);

        var used = _cache.TryUseReady(files[0], m =>
        {
            _cache.ReleaseOwner(Owner);
            _cache.ForceDisposalQueue();
            return Probe(m).IsDisposed;
        }, out var disposedDuringUse);
        _cache.ForceDisposalQueue();

        Assert.True(used);
        Assert.False(disposedDuringUse);
        Assert.True(Probe(model).IsDisposed);
    }

    [Fact]
    public void TryUseReady_FileNotInMemory_DoesNotCallUse()
    {
        var called = false;

        var used = _cache.TryUseReady(Files(1)[0], _ => called = true, out _);

        Assert.False(used);
        Assert.False(called);
    }

    [Fact]
    public void Put_ReplacesCachedImage_AndDisposesThePreviousOne()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var original = new ImageModel { FileInfo = files[0], Image = new ProbeImage() };
        var replacement = new ImageModel { FileInfo = files[0], Image = new ProbeImage() };

        _cache.Put(Owner, files[0], original);
        _cache.Put(Owner, files[0], replacement);
        _cache.ForceDisposalQueue();

        Assert.True(_cache.TryGetReady(files[0], out var ready));
        Assert.Same(replacement, ready);
        Assert.True(Probe(original).IsDisposed);
        Assert.False(Probe(replacement).IsDisposed);
    }

    [Fact]
    public void Put_SameModelAgainAfterItsFileWasInvalidated_KeepsItsImage()
    {
        // A tab still shows the image of a file PicView rewrote, and a relist keeps what the tab shows in the cache
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = new ImageModel { FileInfo = files[0], Image = new ProbeImage() };
        _cache.Put(Owner, files[0], model);

        _cache.Invalidate(files[0].FullName);
        _cache.Put(Owner, files[0], model);
        _cache.ForceDisposalQueue();

        Assert.False(Probe(model).IsDisposed, "The cache still holds the model under its file");
    }

    [Fact]
    public async Task Put_SameModelAgain_LoadsNothingAndKeepsIt()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = new ImageModel { FileInfo = files[0], Image = new ProbeImage() };

        _cache.Put(Owner, files[0], model);
        _cache.Put(Owner, files[0], model);
        _cache.ForceDisposalQueue();

        Assert.Same(model, await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken));
        Assert.False(Probe(model).IsDisposed);
        Assert.False(_loadCounts.ContainsKey(files[0].FullName));
    }

    [Fact]
    public async Task Invalidate_NextRequestLoadsAgain_AndOldImageIsDisposed()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var old = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        _cache.Invalidate(files[0].FullName);
        var fresh = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        _cache.ForceDisposalQueue();

        Assert.True(Probe(old!).IsDisposed);
        Assert.NotSame(old, fresh);
        Assert.Equal(2, _loadCounts[files[0].FullName]);
    }

    [Fact]
    public async Task Invalidate_ImageStillHeldByOwner_IsNotDisposed()
    {
        // A file changed on disk, e.g. by writing its rating, while its tab still shows the old image
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var shown = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        _cache.Invalidate(files[0].FullName);
        _cache.ForceDisposalQueue();

        Assert.False(Probe(shown!).IsDisposed);
    }

    [Fact]
    public async Task ReleaseOwner_ImageSharedWithAnotherOwner_DisposedOnlyAfterBothRelease()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        _cache.SetFiles(OtherOwner, files);
        var model = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        await _cache.GetAsync(OtherOwner, files[0], TestContext.Current.CancellationToken);

        _cache.ReleaseOwner(Owner);
        _cache.ForceDisposalQueue();
        Assert.False(Probe(model!).IsDisposed, "The other owner still holds the image");

        _cache.ReleaseOwner(OtherOwner);
        _cache.ForceDisposalQueue();
        Assert.True(Probe(model!).IsDisposed);
    }

    [Fact]
    public async Task ReleaseImages_ThenRequestedAgain_ServedFromMemoryAndKept()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        _cache.ReleaseImages(Owner);
        var again = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        _cache.ForceDisposalQueue();

        Assert.Same(model, again);
        Assert.Equal(1, _loadCounts[files[0].FullName]);
        Assert.False(Probe(model!).IsDisposed);
    }

    [Fact]
    public async Task SetFiles_FileDroppedFromList_ImageReleased()
    {
        var files = Files(2);
        _cache.SetFiles(Owner, files);
        var dropped = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        var kept = await _cache.GetAsync(Owner, files[1], TestContext.Current.CancellationToken);

        _cache.SetFiles(Owner, [files[1]]);
        _cache.ForceDisposalQueue();

        Assert.True(Probe(dropped!).IsDisposed);
        Assert.False(Probe(kept!).IsDisposed);
    }

    [Fact]
    public async Task SetFiles_ListReordered_ImagesStayHeld()
    {
        var files = Files(3);
        _cache.SetFiles(Owner, files);
        var models = new ImageModel[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            models[i] = (await _cache.GetAsync(Owner, files[i], TestContext.Current.CancellationToken))!;
        }

        _cache.SetFiles(Owner, [files[2], files[0], files[1]]);
        _cache.ForceDisposalQueue();

        Assert.All(models, model => Assert.False(Probe(model).IsDisposed));
    }

    [Fact]
    public async Task Preload_LoadsNeighboursAheadAndBehind()
    {
        var files = Files(15);
        _cache.SetFiles(Owner, files);
        const int current = 7;

        _cache.Preload(Owner, files[current], reversed: false, TestContext.Current.CancellationToken);

        var expected = Enumerable.Range(1, Settings.Navigation.PositiveIterations).Select(i => files[current + i])
            .Concat(Enumerable.Range(1, Settings.Navigation.NegativeIterations).Select(i => files[current - i]))
            .ToArray();
        await WaitUntilReady(expected);
        Assert.False(_cache.TryGetReady(files[current], out _), "Preloading skips the current file");
    }

    [Fact]
    public async Task Preload_OneStepBack_StillLoadsDeepForwards()
    {
        var files = Files(60);
        _cache.SetFiles(Owner, files);
        var (deep, shallow) = (Settings.Navigation.PositiveIterations, Settings.Navigation.NegativeIterations);

        StepTo(files[30], reversed: true);

        await WaitUntilReady(Range(files, 31, deep).Concat(Range(files, 30 - shallow, shallow)));
        Assert.False(_cache.TryGetReady(files[30 - shallow - 1], out _), "A single step back is not travel backwards");
    }

    [Fact]
    public async Task Preload_TwoStepsBack_LoadsDeepBackwardsAndShallowForwards()
    {
        var files = Files(60);
        _cache.SetFiles(Owner, files);
        var (deep, shallow) = (Settings.Navigation.PositiveIterations, Settings.Navigation.NegativeIterations);
        // Each step far from the others, so no preload loads what a later one is asked about
        StepTo(files[50], reversed: true);
        await WaitUntilReady(Range(files, 51, deep));

        StepTo(files[30], reversed: true);

        await WaitUntilReady(Range(files, 30 - deep, deep).Concat(Range(files, 31, shallow)));
        Assert.False(_cache.TryGetReady(files[31 + shallow], out _), "Forwards is behind now, so it is the shallow side");
    }

    [Fact]
    public async Task Preload_TravelingBackwardsThenTwoStepsForwards_LoadsDeepForwardsAgain()
    {
        var files = Files(60);
        _cache.SetFiles(Owner, files);
        var (deep, shallow) = (Settings.Navigation.PositiveIterations, Settings.Navigation.NegativeIterations);
        StepTo(files[55], reversed: true);
        await WaitUntilReady(Range(files, 55 - shallow, shallow));
        StepTo(files[40], reversed: true);
        await WaitUntilReady(Range(files, 40 - deep, deep));

        StepTo(files[20], reversed: false);
        await WaitUntilReady(Range(files, 20 - deep, deep).Concat(Range(files, 21, shallow)));
        Assert.False(_cache.TryGetReady(files[21 + shallow], out _), "A single step forwards is not travel forwards");

        StepTo(files[25], reversed: false);
        await WaitUntilReady(Range(files, 26, deep));
    }

    [Fact]
    public async Task Preload_AskedAgainWithoutAStep_KeepsTheDirectionOfTravel()
    {
        var files = Files(60);
        _cache.SetFiles(Owner, files);
        var (deep, shallow) = (Settings.Navigation.PositiveIterations, Settings.Navigation.NegativeIterations);
        StepTo(files[50], reversed: true);
        await WaitUntilReady(Range(files, 51, deep));

        // As when an archive lists more pages after one step back: a preload again, with no step taken
        _cache.Preload(Owner, files[30], reversed: true, TestContext.Current.CancellationToken);

        await WaitUntilReady(Range(files, 31, deep).Concat(Range(files, 30 - shallow, shallow)));
        Assert.False(_cache.TryGetReady(files[30 - shallow - 1], out _), "Only a step turns the direction of travel");
    }

    [Fact]
    public async Task Preload_TravelResetWhileTravelingBackwards_LoadsDeepForwardsAgain()
    {
        var files = Files(60);
        _cache.SetFiles(Owner, files);
        var (deep, shallow) = (Settings.Navigation.PositiveIterations, Settings.Navigation.NegativeIterations);
        StepTo(files[55], reversed: true);
        await WaitUntilReady(Range(files, 55 - shallow, shallow));
        StepTo(files[40], reversed: true);
        await WaitUntilReady(Range(files, 40 - deep, deep));

        // As when the tab opens another folder
        _cache.ResetTravel(Owner);
        _cache.Preload(Owner, files[15], reversed: false, TestContext.Current.CancellationToken);

        await WaitUntilReady(Range(files, 16, deep).Concat(Range(files, 15 - shallow, shallow)));
        Assert.False(_cache.TryGetReady(files[15 - shallow - 1], out _), "Backwards is behind again, the shallow side");
    }

    [Fact]
    public async Task HoldReady_ImageReleasedByItsOwner_IsNotDisposedUntilTheLeaseEnds()
    {
        var files = Files(1);
        _cache.SetFiles(Owner, files);
        var model = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        var lease = _cache.HoldReady(files[0], out var held);
        _cache.ReleaseOwner(Owner);
        _cache.ForceDisposalQueue();

        Assert.NotNull(lease);
        Assert.Same(model, held);
        Assert.False(Probe(model!).IsDisposed, "Held by the lease");

        lease.Dispose();
        _cache.ForceDisposalQueue();
        Assert.True(Probe(model!).IsDisposed);
    }

    [Fact]
    public void HoldReady_FileNotInMemory_HoldsNothing()
    {
        var lease = _cache.HoldReady(Files(1)[0], out var held);

        Assert.Null(lease);
        Assert.Null(held);
    }

    // A step to a file, then the preload around it, as a tab's iterator does when it shows the next image
    private void StepTo(FileInfo file, bool reversed)
    {
        _cache.NoteStep(Owner, reversed);
        _cache.Preload(Owner, file, reversed, TestContext.Current.CancellationToken);
    }

    private static IEnumerable<FileInfo> Range(FileInfo[] files, int start, int count) =>
        files.Skip(start).Take(count);

    private async Task WaitUntilReady(IEnumerable<FileInfo> files)
    {
        var expected = files.ToArray();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!Array.TrueForAll(expected, f => _cache.TryGetReady(f, out _)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.All(expected, f => Assert.True(_cache.TryGetReady(f, out _), f.Name));
    }

    [Fact]
    public async Task Release_MoreUnreferencedThanTheCap_DisposesTheOldestAndKeepsTheNewest()
    {
        var cache = new SharedImageCache(LoadProbe, new ManualTimeProvider());
        var window = PicView.Core.Preloading.PreLoaderConfig.MaxCount;
        var files = Files(window * 3 + 4);
        cache.SetFiles(Owner, files);

        // As scrolling forwards: every step past the window releases the image farthest behind
        var models = new ImageModel[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            models[i] = (await cache.GetAsync(Owner, files[i], TestContext.Current.CancellationToken))!;
        }

        var released = files.Length - window;
        var disposed = Enumerable.Range(0, files.Length).Where(i => Probe(models[i]).IsDisposed).ToArray();
        Assert.Equal(Enumerable.Range(0, released - window), disposed);
    }

    [Fact]
    public async Task Release_NothingReleasedAfterwards_DisposedOnceTheDelayEnds()
    {
        var clock = new ManualTimeProvider();
        var cache = new SharedImageCache(LoadProbe, clock);
        var files = Files(1);
        cache.SetFiles(Owner, files);
        var model = await cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        cache.ReleaseImages(Owner);

        clock.Advance(TimeSpan.FromSeconds(14));
        Assert.False(Probe(model!).IsDisposed, "Still within the disposal delay");

        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(Probe(model!).IsDisposed, "The delay ended, with no other release to sweep it");
    }

    [Fact]
    public async Task Release_TakenAgainAndReleasedAgain_DelayCountsFromTheLastRelease()
    {
        var clock = new ManualTimeProvider();
        var cache = new SharedImageCache(LoadProbe, clock);
        var files = Files(1);
        cache.SetFiles(Owner, files);
        var model = await cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        cache.ReleaseImages(Owner);

        clock.Advance(TimeSpan.FromSeconds(10));
        await cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);
        cache.ReleaseImages(Owner);

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.False(Probe(model!).IsDisposed, "Only 10 seconds since it was last released");

        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.True(Probe(model!).IsDisposed);
    }

    private ImageModel LoadProbe(FileInfo file)
    {
        _loadCounts.AddOrUpdate(file.FullName, 1, static (_, count) => count + 1);
        return new ImageModel { FileInfo = file, Image = new ProbeImage() };
    }

    private static FileInfo[] Files(int count) =>
        Enumerable.Range(0, count).Select(i => new FileInfo($"test{i}.jpg")).ToArray();

    private static ProbeImage Probe(ImageModel model) => Assert.IsType<ProbeImage>(model.Image);

    private sealed class ProbeImage : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
