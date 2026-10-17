using System.Collections.Concurrent;
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
    public async Task GetAsync_ConcurrentRequestsForSameFile_LoadOnce()
    {
        var gate = new TaskCompletionSource();
        var loads = 0;
        var cache = new SharedImageCache(async f =>
        {
            Interlocked.Increment(ref loads);
            await gate.Task;
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
        var cache = new SharedImageCache(async f =>
        {
            await gate.Task;
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
        _cache.ForceDisposalQueue();
        var fresh = await _cache.GetAsync(Owner, files[0], TestContext.Current.CancellationToken);

        Assert.True(Probe(old!).IsDisposed);
        Assert.NotSame(old, fresh);
        Assert.Equal(2, _loadCounts[files[0].FullName]);
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
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!Array.TrueForAll(expected, f => _cache.TryGetReady(f, out _)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.All(expected, f => Assert.True(_cache.TryGetReady(f, out _), f.Name));
        Assert.False(_cache.TryGetReady(files[current], out _), "Preloading skips the current file");
    }

    private ValueTask<ImageModel> LoadProbe(FileInfo file)
    {
        _loadCounts.AddOrUpdate(file.FullName, 1, static (_, count) => count + 1);
        return ValueTask.FromResult(new ImageModel { FileInfo = file, Image = new ProbeImage() });
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
