using PicView.Core.Generators;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Thumbnailing;

namespace PicView.Tests.Thumbnailing;

public class ThumbnailsTests
{
    private const uint Tab = 7;
    private const uint OtherTab = 8;

    private readonly FakeThumbnailDecoder _decoder = new();
    private readonly SharedImageCache _images = new(static file => new ImageModel { FileInfo = file });
    private readonly Thumbnails _thumbnails;

    public ThumbnailsTests()
    {
        _thumbnails = new Thumbnails(_decoder, _images);
    }

    private static FileInfo File(string name) => new(Path.Combine(Path.GetTempPath(), "picview-thumbnails", name));

    private static FakeThumbnail Fake(object? thumbnail) => Assert.IsType<FakeThumbnail>(thumbnail);

    [Fact]
    public async Task GetAsync_NothingCached_DecodesAtRequestedHeight()
    {
        var thumbnail = Fake(await _thumbnails.GetAsync(File("a.jpg"), 200, Tab));

        Assert.Equal("decode", thumbnail.Source);
        Assert.Equal(200u, thumbnail.Height);
    }

    [Fact]
    public async Task GetAsync_KeptThumbnail_IsServedWithoutDecodingAgain()
    {
        var file = File("a.jpg");
        var first = await _thumbnails.GetAsync(file, 200, Tab);

        var second = await _thumbnails.GetAsync(file, 200, OtherTab);

        Assert.Same(first, second);
        Assert.Single(_decoder.Calls, "decode:a.jpg");
    }

    [Fact]
    public async Task GetAsync_KeptThumbnailIsShorter_DecodesAgainAndKeepsTheTallest()
    {
        var file = File("a.jpg");
        await _thumbnails.GetAsync(file, 150, Tab);

        var tall = Fake(await _thumbnails.GetAsync(file, 300, Tab));
        var shorter = await _thumbnails.GetAsync(file, 150, Tab);

        Assert.Equal(300u, tall.Height);
        Assert.Same(tall, shorter);
        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:a.jpg"));
    }

    [Fact]
    public async Task GetAsync_TallerKeptThumbnail_IsNotReplacedByAShorterOne()
    {
        var file = File("a.jpg");
        var tall = await _thumbnails.GetAsync(file, 300, Tab);

        _thumbnails.Keep(OtherTab, file.FullName, new FakeThumbnail("kept", file.FullName, 100), 100);

        Assert.Same(tall, await _thumbnails.GetAsync(file, 300, Tab));
    }

    [Fact]
    public async Task GetAsync_NoOwner_IsNotKept()
    {
        var file = File("a.jpg");
        await _thumbnails.GetAsync(file, 200, owner: null);

        await _thumbnails.GetAsync(file, 200, owner: null);

        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:a.jpg"));
    }

    [Fact]
    public async Task GetAsync_StillBitmapInImageCache_ScalesItDownInsteadOfDecoding()
    {
        var file = File("a.jpg");
        _images.Put(Tab, file, new ImageModel { FileInfo = file, Image = "full", ImageType = ImageType.Bitmap });

        var thumbnail = Fake(await _thumbnails.GetAsync(file, 200, Tab));

        Assert.Equal("scale", thumbnail.Source);
        Assert.Equal(200u, thumbnail.Height);
        Assert.DoesNotContain("decode:a.jpg", _decoder.Calls);
    }

    [Theory]
    [InlineData(ImageType.Svg)]
    [InlineData(ImageType.AnimatedGif)]
    [InlineData(ImageType.AnimatedWebp)]
    [InlineData(ImageType.MotionPhoto)]
    public async Task GetAsync_ImageInCacheIsNotAStillBitmap_Decodes(ImageType type)
    {
        var file = File("a.svg");
        _images.Put(Tab, file, new ImageModel { FileInfo = file, Image = "full", ImageType = type });

        var thumbnail = Fake(await _thumbnails.GetAsync(file, 200, Tab));

        Assert.Equal("decode", thumbnail.Source);
        Assert.DoesNotContain("scale", _decoder.Calls);
    }

    [Fact]
    public async Task GetAsync_ScalingDownGivesNothing_Decodes()
    {
        var file = File("a.jpg");
        _images.Put(Tab, file, new ImageModel { FileInfo = file, Image = "full", ImageType = ImageType.Bitmap });
        _decoder.CanScaleDown = false;

        var thumbnail = Fake(await _thumbnails.GetAsync(file, 200, Tab));

        Assert.Equal("decode", thumbnail.Source);
    }

    [Fact]
    public async Task GetAsync_FileGivesNoThumbnail_ReturnsNullAndKeepsNothing()
    {
        var file = File("broken.jpg");
        _decoder.CanDecode = false;

        Assert.Null(await _thumbnails.GetAsync(file, 200, Tab));
        _decoder.CanDecode = true;

        Assert.NotNull(await _thumbnails.GetAsync(file, 200, Tab));
        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:broken.jpg"));
    }

    [Fact]
    public async Task GetAsync_ManyAtOnce_DecodeNoMoreThanTheSlots()
    {
        var thumbnails = new Thumbnails(_decoder, _images, slots: 2);
        using var gate = new ManualResetEventSlim(false);
        _decoder.DecodeGate = gate;

        var requests = Enumerable.Range(0, 6)
            .Select(i => thumbnails.GetAsync(File($"{i}.jpg"), 200, Tab).AsTask())
            .ToArray();
        // Hold the decodes until both slots are taken, so a missing throttle would show as a third
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_decoder.MostDecodesInFlight < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
        gate.Set();
        await Task.WhenAll(requests);

        Assert.Equal(2, _decoder.MostDecodesInFlight);
    }

    [Fact]
    public async Task GetAsync_CancelledWhileWaitingForASlot_ReturnsNullWithoutDecoding()
    {
        var thumbnails = new Thumbnails(_decoder, _images, slots: 1);
        using var gate = new ManualResetEventSlim(false);
        _decoder.DecodeGate = gate;
        var busy = thumbnails.GetAsync(File("busy.jpg"), 200, Tab).AsTask();
        using var cts = new CancellationTokenSource();

        var waiting = thumbnails.GetAsync(File("waiting.jpg"), 200, Tab, cts.Token).AsTask();
        await cts.CancelAsync();

        Assert.Null(await waiting);
        gate.Set();
        await busy;
        Assert.DoesNotContain("decode:waiting.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetAsync_CancelledWhileWaitingForASlot_LeavesTheSlotToTheNextRequest()
    {
        var thumbnails = new Thumbnails(_decoder, _images, slots: 1);
        using var gate = new ManualResetEventSlim(false);
        _decoder.DecodeGate = gate;
        var busy = thumbnails.GetAsync(File("busy.jpg"), 200, Tab).AsTask();
        using var cts = new CancellationTokenSource();
        var cancelled = thumbnails.GetAsync(File("cancelled.jpg"), 200, Tab, cts.Token).AsTask();
        var next = thumbnails.GetAsync(File("next.jpg"), 200, Tab).AsTask();

        await cts.CancelAsync();
        Assert.Null(await cancelled);
        gate.Set();
        await busy;

        Assert.NotNull(await next);
        Assert.DoesNotContain("decode:cancelled.jpg", _decoder.Calls);
        Assert.Equal(1, _decoder.MostDecodesInFlight);
    }

    [Fact]
    public async Task GetAsync_AlreadyCancelled_ReturnsNullWithoutDecoding()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Assert.Null(await _thumbnails.GetAsync(File("a.jpg"), 200, Tab, cts.Token));
        Assert.DoesNotContain("decode:a.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetQuickAsync_NothingKept_ReturnsTheReadyMadeThumbnailWithoutKeepingIt()
    {
        var file = File("a.jpg");

        var quick = Fake(await _thumbnails.GetQuickAsync(file, 200));
        var made = Fake(await _thumbnails.GetAsync(file, 200, Tab));

        Assert.Equal("ready", quick.Source);
        Assert.Equal("decode", made.Source);
    }

    [Fact]
    public async Task GetQuickAsync_NothingReadyMade_NeverDecodes()
    {
        _decoder.HasReadyMade = false;

        Assert.Null(await _thumbnails.GetQuickAsync(File("a.jpg"), 200));
        Assert.DoesNotContain("decode:a.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetQuickAsync_AThumbnailIsKept_ReturnsItWhateverItsHeight()
    {
        var file = File("a.jpg");
        var kept = await _thumbnails.GetAsync(file, 100, Tab);

        Assert.Same(kept, await _thumbnails.GetQuickAsync(file, 300));
        Assert.DoesNotContain("ready:a.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task Release_DropsOnlyThumbnailsNoOtherOwnerKeeps()
    {
        var shared = File("shared.jpg");
        var own = File("own.jpg");
        await _thumbnails.GetAsync(shared, 200, Tab);
        await _thumbnails.GetAsync(shared, 200, OtherTab);
        await _thumbnails.GetAsync(own, 200, Tab);

        _thumbnails.Release(Tab);
        await _thumbnails.GetAsync(shared, 200, OtherTab);
        await _thumbnails.GetAsync(own, 200, OtherTab);

        Assert.Single(_decoder.Calls, "decode:shared.jpg");
        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:own.jpg"));
    }

    [Fact]
    public async Task Forget_DropsTheThumbnailWhoeverKeepsIt()
    {
        var file = File("a.jpg");
        await _thumbnails.GetAsync(file, 200, Tab);
        await _thumbnails.GetAsync(file, 200, OtherTab);

        _thumbnails.Forget(file.FullName);
        await _thumbnails.GetAsync(file, 200, Tab);

        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:a.jpg"));
    }

    [Fact]
    public async Task Forget_WhileTheFileDecodes_TheStaleThumbnailIsNotKept()
    {
        var file = File("a.jpg");
        using var gate = new ManualResetEventSlim(false);
        _decoder.DecodeGate = gate;
        var making = _thumbnails.GetAsync(file, 200, Tab).AsTask();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_decoder.MostDecodesInFlight < 1 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        _thumbnails.Forget(file.FullName);
        gate.Set();
        Assert.NotNull(await making);
        await _thumbnails.GetAsync(file, 200, Tab);

        Assert.Equal(2, _decoder.Calls.Count(c => c == "decode:a.jpg"));
    }

    [Fact]
    public async Task Keep_StoresAThumbnailUnderAPath()
    {
        var file = File("renamed.jpg");
        var thumbnail = new FakeThumbnail("kept", file.FullName, 200);

        _thumbnails.Keep(Tab, file.FullName, thumbnail, 200);

        Assert.Same(thumbnail, await _thumbnails.GetAsync(file, 200, Tab));
        Assert.Empty(_decoder.Calls);
    }

    [Fact]
    public async Task AppOwner_IsNoTabsId_SoReleasingATabKeepsItsThumbnails()
    {
        var file = File("history.jpg");
        await _thumbnails.GetAsync(file, 150, Thumbnails.AppOwner);

        _thumbnails.Release(Tab);

        Assert.NotEqual(Thumbnails.AppOwner, TabIDGenerator.GetNextId());
        await _thumbnails.GetAsync(file, 150, Thumbnails.AppOwner);
        Assert.Single(_decoder.Calls, "decode:history.jpg");
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("The condition did not hold within 5 seconds");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GetTileAsync_NothingKept_DecodesWithoutReadyMadeAndLeavesTheSmallStoreAlone()
    {
        var file = File("a.jpg");

        var tile = Fake(await _thumbnails.GetTileAsync(file, 600));

        Assert.Equal("decode-full", tile.Source);
        Assert.Equal(600u, tile.Height);
        Assert.Equal(["decode-full:a.jpg"], _decoder.Calls);
        Assert.True(_thumbnails.TryGetKept(file, out var kept, out var tileHeight));
        Assert.Same(tile, kept);
        Assert.Equal(600u, tileHeight);
        // The small store has nothing: a small request still decodes the ordinary way
        await _thumbnails.GetAsync(file, 150, Tab);
        Assert.Contains("decode:a.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetTileAsync_KeptForAtLeastTheHeight_IsServedWithoutDecoding()
    {
        var file = File("a.jpg");
        var first = await _thumbnails.GetTileAsync(file, 600);

        var request = _thumbnails.GetTileAsync(file, 400);

        Assert.True(request.IsCompleted);
        Assert.Same(first, request.Result);
        Assert.Single(_decoder.Calls);
    }

    [Fact]
    public async Task GetTileAsync_TallerThanKept_DecodesAndReplaces()
    {
        var file = File("a.jpg");
        await _thumbnails.GetTileAsync(file, 400);

        var taller = Fake(await _thumbnails.GetTileAsync(file, 600));

        Assert.Equal(600u, taller.Height);
        Assert.Equal(2, _decoder.Calls.Count(c => string.Equals(c, "decode-full:a.jpg", StringComparison.Ordinal)));
        Assert.True(_thumbnails.TryGetKept(file, out var kept, out _));
        Assert.Same(taller, kept);
    }

    [Fact]
    public async Task GetTileAsync_StillBitmapInImageCache_ScalesItDownInsteadOfDecoding()
    {
        var file = File("a.jpg");
        _images.Put(Tab, file, new ImageModel { FileInfo = file, Image = "full", ImageType = ImageType.Bitmap });

        var tile = Fake(await _thumbnails.GetTileAsync(file, 600));

        Assert.Equal("scale", tile.Source);
        Assert.DoesNotContain("decode-full:a.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetTileAsync_FileGivesNone_KeepsNothing()
    {
        _decoder.CanDecode = false;
        var file = File("a.jpg");

        Assert.Null(await _thumbnails.GetTileAsync(file, 600));
        Assert.False(_thumbnails.TryGetKept(file, out _, out _));
    }

    [Fact]
    public async Task GetTileAsync_PastTheBudget_DropsTheOldestButKeepsItsSmallThumbnail()
    {
        // FakeThumbnailDecoder.BytesPerHeight is 1000, so a 600-tall fake is 600,000 bytes
        var thumbnails = new Thumbnails(_decoder, _images, tileBudgetInBytes: 1_000_000);
        var a = File("a.jpg");
        var small = await thumbnails.GetAsync(a, 150, Tab);
        await thumbnails.GetTileAsync(a, 600);

        await thumbnails.GetTileAsync(File("b.jpg"), 600);

        Assert.True(thumbnails.TryGetKept(a, out var kept, out var tileHeight));
        Assert.Same(small, kept);
        Assert.Equal(0u, tileHeight);
    }

    [Fact]
    public async Task Forget_DropsTheTileThumbnailAndTheSmallOne()
    {
        var file = File("a.jpg");
        await _thumbnails.GetAsync(file, 150, Tab);
        await _thumbnails.GetTileAsync(file, 600);

        _thumbnails.Forget(file.FullName);

        Assert.False(_thumbnails.TryGetKept(file, out _, out _));
    }

    [Fact]
    public async Task GetTileAsync_FileForgottenWhileItWasMade_IsHandedBackButNotKept()
    {
        var file = File("a.jpg");
        using var gate = new ManualResetEventSlim();
        _decoder.DecodeGate = gate;
        var request = _thumbnails.GetTileAsync(file, 600).AsTask();
        await WaitUntil(() => _decoder.Calls.Contains("decode-full:a.jpg"));

        _thumbnails.Forget(file.FullName);
        gate.Set();

        Assert.NotNull(await request);
        Assert.False(_thumbnails.TryGetKept(file, out _, out _));
    }

    [Fact]
    public async Task Relocate_MovesBothKindsToTheNewPath()
    {
        var oldFile = File("a.jpg");
        var newFile = new FileInfo(Path.Combine(Path.GetTempPath(), "picview-renamed", "a.jpg"));
        var small = await _thumbnails.GetAsync(oldFile, 150, Tab);
        var tile = await _thumbnails.GetTileAsync(oldFile, 600);

        _thumbnails.Relocate(Tab, oldFile.FullName, newFile.FullName);

        Assert.True(_thumbnails.TryGetKept(newFile, out var kept, out var tileHeight));
        Assert.Same(tile, kept);
        Assert.Equal(600u, tileHeight);
        Assert.Same(small, await _thumbnails.GetAsync(newFile, 150, Tab));
        Assert.Equal(1, _decoder.Calls.Count(c => c == "decode:a.jpg"));
    }

    [Fact]
    public async Task GetTileAsync_CancelledWhileWaitingForASlot_ReturnsNullWithoutDecoding()
    {
        var thumbnails = new Thumbnails(_decoder, _images, slots: 1);
        using var gate = new ManualResetEventSlim();
        _decoder.DecodeGate = gate;
        var first = thumbnails.GetTileAsync(File("a.jpg"), 600).AsTask();
        using var cts = new CancellationTokenSource();
        var waiting = thumbnails.GetTileAsync(File("b.jpg"), 600, cts.Token).AsTask();

        await cts.CancelAsync();

        Assert.Null(await waiting);
        gate.Set();
        await first;
        Assert.DoesNotContain("decode-full:b.jpg", _decoder.Calls);
    }

    [Fact]
    public async Task GetTileAsync_ShorterMakeFinishesAfterTaller_TheTallerStaysKept()
    {
        var file = File("a.jpg");
        using var gate = new ManualResetEventSlim();
        _decoder.DecodeGate = gate;
        var shorter = _thumbnails.GetTileAsync(file, 400).AsTask();
        await WaitUntil(() => _decoder.Calls.Contains("decode-full:a.jpg"));
        _decoder.DecodeGate = null; // the taller make is not held, so it finishes first
        var taller = Fake(await _thumbnails.GetTileAsync(file, 600));

        gate.Set();

        Assert.Equal(400u, Fake(await shorter).Height);
        Assert.True(_thumbnails.TryGetKept(file, out var kept, out var height));
        Assert.Equal(600u, height);
        Assert.Same(taller, kept);
    }
}
