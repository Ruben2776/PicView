using Avalonia;
using Avalonia.Animation;
using Avalonia.Media.Imaging;

namespace PicView.Avalonia.AnimatedImage;

public interface IGifInstance : IDisposable
{
    IterationCount IterationCount { get; set; }
    bool AutoStart { get; }
    CancellationTokenSource CurrentCts { get; }
    int GifFrameCount { get; }
    PixelSize GifPixelSize { get; }
    bool IsDisposed { get; }
    WriteableBitmap? ProcessFrameTime(TimeSpan stopwatchElapsed);
}
