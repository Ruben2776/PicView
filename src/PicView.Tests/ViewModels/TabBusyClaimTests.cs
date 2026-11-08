using ImageMagick;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;

namespace PicView.Tests.ViewModels;

/// <summary>
/// A tab is Busy while an operation on it holds a busy claim, and stops being Busy when the last claim ends, however
/// the operation ends.
/// </summary>
[Collection("Sequential")]
public class TabBusyClaimTests : IDisposable
{
    private readonly TabViewModel _tab;

    public TabBusyClaimTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        _tab = NewTab();
    }

    private static TabViewModel NewTab() =>
        new(null!, TestTabServices.For(new SharedImageCache(_ => ValueTask.FromResult(new ImageModel())!),
            new ThumbnailCache(), new MockThumbnailLoader()));

    public void Dispose() => _tab.Dispose();

    [Fact]
    public void NewTab_IsNotBusy() => Assert.False(_tab.IsBusy.Value);

    [Fact]
    public void ClaimBusy_WhileHeld_TabIsBusy()
    {
        using var busy = _tab.ClaimBusy();
        Assert.True(_tab.IsBusy.Value);
    }

    [Fact]
    public void ClaimBusy_WhenReleased_TabIsNotBusy()
    {
        _tab.ClaimBusy().Dispose();
        Assert.False(_tab.IsBusy.Value);
    }

    [Fact]
    public void ClaimBusy_TwoOverlapping_BusyUntilTheLastEnds()
    {
        var first = _tab.ClaimBusy();
        var second = _tab.ClaimBusy();

        first.Dispose();
        var busyAfterFirst = _tab.IsBusy.Value;
        second.Dispose();

        Assert.True(busyAfterFirst);
        Assert.False(_tab.IsBusy.Value);
    }

    [Fact]
    public void ClaimBusy_DisposedTwice_ReleasesOnce()
    {
        var first = _tab.ClaimBusy();
        using var second = _tab.ClaimBusy();

        first.Dispose();
        first.Dispose();

        Assert.True(_tab.IsBusy.Value);
    }

    [Fact]
    public void ClaimBusy_UsingBlockThrows_ReleasesTheClaim()
    {
        Action operation = () =>
        {
            using var busy = _tab.ClaimBusy();
            throw new InvalidOperationException();
        };

        Assert.Throws<InvalidOperationException>(operation);

        Assert.False(_tab.IsBusy.Value);
    }

    [Fact]
    public async Task ClaimBusy_ManyClaimsAcrossThreads_EndsNotBusy()
    {
        var seen = new List<bool>();
        using var watching = _tab.IsBusy.AsObservable().Subscribe(v =>
        {
            lock (seen)
            {
                seen.Add(v);
            }
        });

        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            using var busy = _tab.ClaimBusy();
        })));

        Assert.False(_tab.IsBusy.Value);
        lock (seen)
        {
            Assert.False(seen[^1]);
        }
    }

    [Fact]
    public async Task ClaimBusy_ClaimedWhileAReleaseIsStillPublishing_PublishesBusyLast()
    {
        var held = _tab.ClaimBusy();
        IDisposable? second = null;
        Task? claiming = null;
        var seen = new List<bool>();
        // The release's notification is slow: while it is still being delivered, another thread claims the tab, as a
        // release preempted between storing the value and notifying would be
        using var watching = _tab.IsBusy.AsObservable().Subscribe(v =>
        {
            if (!v && claiming is null)
            {
                using var claimed = new ManualResetEventSlim();
                claiming = Task.Run(() =>
                {
                    second = _tab.ClaimBusy();
                    claimed.Set();
                });
                claimed.Wait(TimeSpan.FromMilliseconds(500));
            }

            lock (seen)
            {
                seen.Add(v);
            }
        });

        held.Dispose();
        await claiming!;

        Assert.True(_tab.IsBusy.Value);
        lock (seen)
        {
            Assert.True(seen[^1]);
        }

        second!.Dispose();
    }

    [Fact]
    public void ClaimBusy_ReleasedAfterTabDisposed_DoesNotThrow()
    {
        var tab = NewTab();
        var busy = tab.ClaimBusy();
        tab.Dispose();

        var thrown = Record.Exception(busy.Dispose);

        Assert.Null(thrown);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
