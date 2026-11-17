using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.AnimatedImage;

namespace PicView.Tests.AnimatedImage;

internal sealed class FakeAnimationDecoder(params int[] durationsMs) : IAnimationDecoder
{
    public List<int> Rendered { get; } = [];

    public bool IsDisposed { get; private set; }

    public bool ThrowOnRender { get; set; }

    public PixelSize Size => new(4, 4);

    public AlphaFormat AlphaFormat => AlphaFormat.Opaque;

    public IReadOnlyList<TimeSpan> FrameDurations { get; } =
        durationsMs.Select(ms => TimeSpan.FromMilliseconds(ms)).ToArray();

    public void RenderFrame(int index, WriteableBitmap target)
    {
        if (ThrowOnRender)
        {
            throw new InvalidOperationException("Render failed.");
        }

        Rendered.Add(index);
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
