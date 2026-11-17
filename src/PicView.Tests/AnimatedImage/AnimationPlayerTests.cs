using Avalonia;
using PicView.Avalonia.AnimatedImage;
using PicView.Core.Animation;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

public class AnimationPlayerTests
{
    private static AnimationPlayback NewPlayback() => new(new FileInfo("a.gif"), ImageType.AnimatedGif);

    [AvaloniaFact]
    public void Constructor_AttachesTimelineAndDrawsFirstFrame()
    {
        var playback = NewPlayback();
        var decoder = new FakeAnimationDecoder(100, 100, 100);

        using var player = new AnimationPlayer(playback, decoder);

        Assert.Equal(3, playback.Timeline!.Count);
        Assert.Equal([0], decoder.Rendered);
        Assert.Equal(new PixelSize(4, 4), player.Size);
        Assert.Same(playback, player.Playback);
    }

    [AvaloniaFact]
    public void Frame_SameFrame_DoesNotDrawAgain()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);

        player.Frame(TimeSpan.FromMilliseconds(50));
        player.Frame(TimeSpan.Zero);

        Assert.Equal([0], decoder.Rendered);
    }

    [AvaloniaFact]
    public void Frame_NextFrame_DrawsItOnce()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);

        var first = player.Frame(TimeSpan.FromMilliseconds(150));
        var second = player.Frame(TimeSpan.Zero);

        Assert.Equal([0, 1], decoder.Rendered);
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Frame_WhilePaused_DrawsNothingNew()
    {
        var playback = NewPlayback();
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(playback, decoder);

        playback.SetPaused(true);
        player.Frame(TimeSpan.FromMilliseconds(250));

        Assert.Equal([0], decoder.Rendered);
    }

    [AvaloniaFact]
    public void Frame_AfterStepBack_DrawsTheEarlierFrame()
    {
        var playback = NewPlayback();
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(playback, decoder);

        player.Frame(TimeSpan.FromMilliseconds(250));
        Assert.Equal(2, decoder.Rendered[^1]);

        playback.Step(-1);
        player.Frame(TimeSpan.Zero);

        Assert.Equal(1, decoder.Rendered[^1]);
    }

    [AvaloniaFact]
    public void Frame_SingleFrameDecoder_KeepsDrawingFrameZero()
    {
        var decoder = new FakeAnimationDecoder(100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);

        var bitmap = player.Frame(TimeSpan.FromSeconds(5));

        Assert.NotNull(bitmap);
        Assert.Equal([0], decoder.Rendered);
    }

    [AvaloniaFact]
    public void Frame_DecoderWithNoFrames_ReturnsBitmapWithoutThrowing()
    {
        var decoder = new FakeAnimationDecoder();
        using var player = new AnimationPlayer(NewPlayback(), decoder);

        var bitmap = player.Frame(TimeSpan.FromSeconds(1));

        Assert.NotNull(bitmap);
        Assert.Empty(decoder.Rendered);
    }

    [AvaloniaFact]
    public void Frame_DecoderThrows_ReturnsLastBitmapAndDoesNotThrow()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);
        var before = player.Frame(TimeSpan.Zero);

        decoder.ThrowOnRender = true;
        var after = player.Frame(TimeSpan.FromMilliseconds(150));

        Assert.NotNull(after);
        Assert.Same(before, after);
    }

    [AvaloniaFact]
    public void Frame_AllocatesNothing_WhenFrameIsUnchanged()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);
        player.Frame(TimeSpan.Zero);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            player.Frame(TimeSpan.Zero);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [AvaloniaFact]
    public void Dispose_DisposesDecoderAndStream_AndFrameReturnsNull()
    {
        var decoder = new FakeAnimationDecoder(100, 100);
        var stream = new MemoryStream();
        var player = new AnimationPlayer(NewPlayback(), decoder, stream);

        player.Dispose();

        Assert.True(player.IsDisposed);
        Assert.True(decoder.IsDisposed);
        Assert.False(stream.CanRead);
        Assert.Null(player.Frame(TimeSpan.FromMilliseconds(10)));
    }

    [AvaloniaFact]
    public void Dispose_Twice_IsHarmless()
    {
        var player = new AnimationPlayer(NewPlayback(), new FakeAnimationDecoder(100, 100));

        player.Dispose();
        var exception = Record.Exception(player.Dispose);

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Open_MissingFile_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gif");
        var playback = new AnimationPlayback(new FileInfo(path), ImageType.AnimatedGif);

        Assert.Throws<FileNotFoundException>(() => AnimationPlayer.Open(playback));
    }
}
