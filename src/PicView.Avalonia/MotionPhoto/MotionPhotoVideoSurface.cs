using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.MotionPhoto;

/// <summary>
/// Renders motion photo video frames supplied by libvlc software video callbacks
/// (MediaPlayer.SetVideoCallbacks). Frames arrive as BGRA32 ("RV32") bytes and are drawn
/// letterboxed into the control. This works on every display stack, including Wayland
/// (where native child-window embedding is impossible with libvlc 3.x), and lets the
/// video participate in the normal Avalonia compositor (zoom, rotation, overlays).
/// </summary>
public sealed class MotionPhotoVideoSurface : Control
{
    private WriteableBitmap? _frameBitmap;

    /// <summary>
    /// Ensures the frame bitmap matches the given video size. Must be called on the UI thread.
    /// </summary>
    public void EnsureBitmap(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (_frameBitmap is { PixelSize.Width: var w, PixelSize.Height: var h } && w == width && h == height)
        {
            return;
        }

        _frameBitmap?.Dispose();
        _frameBitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
    }

    /// <summary>
    /// Copies a BGRA32 frame from an unmanaged buffer into the bitmap and invalidates
    /// the visual. UI thread only; the source must stay valid for the duration of the call.
    /// </summary>
    public unsafe void UpdateFrame(IntPtr bgra, int byteCount, int width, int height)
    {
        try
        {
            EnsureBitmap(width, height);
            var bitmap = _frameBitmap;
            if (bitmap is null || bgra == IntPtr.Zero)
            {
                return;
            }

            var srcRowBytes = width * 4;
            if (byteCount < srcRowBytes * height)
            {
                return;
            }

            using var framebuffer = bitmap.Lock();
            var dstRowBytes = framebuffer.RowBytes;
            var src = (byte*)bgra;
            var dst = (byte*)framebuffer.Address;
            if (dstRowBytes == srcRowBytes)
            {
                Buffer.MemoryCopy(src, dst, (long)dstRowBytes * height, byteCount);
            }
            else
            {
                // Copy row by row to handle potential framebuffer padding
                for (var y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(src + (long)y * srcRowBytes, dst + (long)y * dstRowBytes,
                        dstRowBytes, srcRowBytes);
                }
            }

            InvalidateVisual();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoVideoSurface), nameof(UpdateFrame), e);
        }
    }

    /// <summary>
    /// Drops the current frame so the surface renders nothing.
    /// </summary>
    public void Clear()
    {
        _frameBitmap?.Dispose();
        _frameBitmap = null;
        InvalidateVisual();
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);

        var bitmap = _frameBitmap;
        if (bitmap is null)
        {
            return;
        }

        var viewPort = new Rect(Bounds.Size);
        var sourceSize = bitmap.Size;
        var scale = Stretch.Uniform.CalculateScaling(Bounds.Size, sourceSize);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort
            .CenterRect(new Rect(scaledSize))
            .Intersect(viewPort);
        var sourceRect = new Rect(sourceSize)
            .CenterRect(new Rect(destRect.Size / scale));

        context.DrawImage(bitmap, sourceRect, destRect);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Clear();
        base.OnDetachedFromVisualTree(e);
    }
}
