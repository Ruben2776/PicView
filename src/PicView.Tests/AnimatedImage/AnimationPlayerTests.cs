using Avalonia;
using ImageMagick;
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
    public void Constructor_FirstFrameThrows_ThrowsAndDisposesWhatItOwns()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100) { ThrowOnRender = true };
        var stream = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() => new AnimationPlayer(NewPlayback(), decoder, stream));

        Assert.True(decoder.IsDisposed);
        Assert.False(stream.CanRead);
    }

    [AvaloniaFact]
    public void Frame_DecoderThrows_KeepsLastDrawnFrameUntilANewOneDraws()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);
        var before = player.Frame(TimeSpan.Zero);

        decoder.ThrowOnRender = true;
        var after = player.Frame(TimeSpan.FromMilliseconds(150));

        Assert.NotNull(after);
        Assert.Same(before, after);
        Assert.Equal([0], decoder.Rendered);

        decoder.ThrowOnRender = false;
        player.Frame(TimeSpan.FromMilliseconds(100));

        Assert.Equal([0, 2], decoder.Rendered);
    }

    [AvaloniaFact]
    public void Frame_FailedFrame_IsAskedForOnce()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);

        decoder.ThrowOnRender = true;
        player.Frame(TimeSpan.FromMilliseconds(150));
        for (var i = 0; i < 10; i++)
        {
            player.Frame(TimeSpan.Zero);
        }

        Assert.Equal([0, 1], decoder.Asked);
    }

    [AvaloniaFact]
    public void Frame_FailedFrameComesBack_IsAskedForAgain()
    {
        var playback = NewPlayback();
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(playback, decoder);

        decoder.ThrowOnRender = true;
        player.Frame(TimeSpan.FromMilliseconds(150));
        player.Frame(TimeSpan.FromMilliseconds(100));
        playback.Step(-1);
        player.Frame(TimeSpan.Zero);

        Assert.Equal([0, 1, 2, 1], decoder.Asked);
    }

    [AvaloniaFact]
    public void Frame_AllocatesNothing_WhenFrameHasFailed()
    {
        var decoder = new FakeAnimationDecoder(100, 100, 100);
        using var player = new AnimationPlayer(NewPlayback(), decoder);
        decoder.ThrowOnRender = true;
        player.Frame(TimeSpan.FromMilliseconds(150));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            player.Frame(TimeSpan.Zero);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
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

    [AvaloniaFact]
    public void Open_FileMovedAndDeletedWhileOpen_BothSucceed()
    {
        var path = CreateAnimatedGif();
        var moved = path + ".moved.gif";
        try
        {
            using var player = AnimationPlayer.Open(new AnimationPlayback(new FileInfo(path), ImageType.AnimatedGif));

            File.Move(path, moved);
            File.Delete(moved);

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(moved));
        }
        finally
        {
            File.Delete(path);
            File.Delete(moved);
        }
    }

    [AvaloniaFact]
    public void Open_NotAnImage_ThrowsAndReleasesTheFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gif");
        File.WriteAllText(path, "This is not a GIF.");
        try
        {
            var playback = new AnimationPlayback(new FileInfo(path), ImageType.AnimatedGif);

            Assert.ThrowsAny<Exception>(() => AnimationPlayer.Open(playback));

            // Opening without sharing fails while any other handle to the file is still open
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateAnimatedGif()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gif");
        using var collection = new MagickImageCollection();
        foreach (var color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            collection.Add(new MagickImage(color, 16, 16) { AnimationDelay = 10, Format = MagickFormat.Gif });
        }

        collection.Write(path, MagickFormat.Gif);
        return path;
    }
}
