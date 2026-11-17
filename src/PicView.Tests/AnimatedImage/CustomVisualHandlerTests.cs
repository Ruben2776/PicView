using PicView.Avalonia.AnimatedImage;
using PicView.Core.Animation;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

public class CustomVisualHandlerTests
{
    private static AnimationPlayer NewPlayer() =>
        new(new AnimationPlayback(new FileInfo("a.gif"), ImageType.AnimatedGif), new FakeAnimationDecoder(100, 100));

    [AvaloniaFact]
    public void OnMessage_Player_ReplacesAndDisposesThePreviousOne()
    {
        var handler = new CustomVisualHandler();
        var first = NewPlayer();
        var second = NewPlayer();

        handler.OnMessage(first);
        handler.OnMessage(second);

        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        second.Dispose();
    }

    [AvaloniaFact]
    public void OnMessage_Close_DisposesThePlayer()
    {
        var handler = new CustomVisualHandler();
        var player = NewPlayer();
        handler.OnMessage(player);

        handler.OnMessage(CustomVisualHandler.CloseMessage);

        Assert.True(player.IsDisposed);
    }

    [AvaloniaFact]
    public void OnMessage_CloseWithNoPlayer_DoesNothing()
    {
        var handler = new CustomVisualHandler();

        var exception = Record.Exception(() => handler.OnMessage(CustomVisualHandler.CloseMessage));

        Assert.Null(exception);
    }
}
