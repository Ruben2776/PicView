using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Draws the frames of one <see cref="AnimationPlayer" /> on the compositor thread. A player sent as a message
///     replaces the current one; <see cref="CloseMessage" /> ends it. Between <see cref="StopMessage" /> and
///     <see cref="StartMessage" /> the playback stands still and no frame is asked for. The handler is the one that disposes a player it
///     was given, so nothing is disposed while it draws.
/// </summary>
public class CustomVisualHandler : CompositionCustomVisualHandler
{
    public static readonly object StopMessage = new(),
        StartMessage = new(),
        CloseMessage = new();

    private TimeSpan? _lastServerTime;
    private AnimationPlayer? _player;
    private bool _running;

    public override void OnMessage(object message)
    {
        if (message == StartMessage)
        {
            _running = true;
            _lastServerTime = null;
            RegisterForNextAnimationFrameUpdate();
        }
        else if (message == StopMessage)
        {
            _running = false;
        }
        else if (message == CloseMessage)
        {
            if (_player is null)
            {
                return;
            }

            _player.Dispose();
            _player = null;
            if (_running)
            {
                Invalidate();
            }
        }
        else if (message is AnimationPlayer player)
        {
            _player?.Dispose();
            _player = player;
            _lastServerTime = null;
            if (_running)
            {
                RegisterForNextAnimationFrameUpdate();
            }
        }
    }

    public override void OnAnimationFrameUpdate()
    {
        if (!_running || _player is null)
        {
            return;
        }

        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        if (_player is null)
        {
            return;
        }

        // While stopped the frame is still drawn when something else redraws the visual, but no time passes for it
        var now = CompositionNow;
        var elapsed = _running && _lastServerTime is { } last ? now - last : TimeSpan.Zero;
        _lastServerTime = _running ? now : null;

        try
        {
            var bitmap = _player.Frame(elapsed);
            if (bitmap is not null)
            {
                drawingContext.DrawBitmap(bitmap, new Rect(_player.Size.ToSize(1)), GetRenderBounds());
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(CustomVisualHandler), nameof(OnRender), e);
        }
    }
}
