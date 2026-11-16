using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Thumbnailing;

namespace PicView.Tests.Thumbnailing;

/// <summary>
/// Makes real bitmaps a gallery item can show, without reading any file, and records what it was asked as
/// <see cref="FakeThumbnailDecoder"/> does. Every bitmap is the height asked for and half as wide again. Decoding
/// without the ready-made sources can be turned off, capped, or held until a test lets it go, and so can decoding.
/// </summary>
public sealed class BitmapThumbnailDecoder : IThumbnailDecoder
{
    private int _inFlight;

    public bool CanDecodeFull { get; set; } = true;

    public bool HasReadyMade { get; set; } = true;

    /// <summary>
    /// When set, <see cref="DecodeWithoutReadyMade"/> waits for it before it returns, so a test can hold a Tile
    /// Thumbnail in its slot.
    /// </summary>
    public ManualResetEventSlim? DecodeFullGate { get; set; }

    /// <summary>
    /// When set, <see cref="Decode"/> waits for it before it returns, so a test can hold a small thumbnail in its slot.
    /// </summary>
    public ManualResetEventSlim? DecodeGate { get; set; }

    /// <summary>
    /// When set, <see cref="DecodeWithoutReadyMade"/> gives a bitmap no taller than this, as a source that gives less
    /// than it was asked for does.
    /// </summary>
    public uint? FullHeightCap { get; set; }

    public ConcurrentQueue<string> Calls { get; } = new();

    /// <summary>How many calls are running now.</summary>
    public int InFlight => Volatile.Read(ref _inFlight);

    public object? GetReadyMade(FileInfo file, uint height)
    {
        Calls.Enqueue($"ready:{file.Name}");
        return HasReadyMade ? Bitmap(height) : null;
    }

    public object? Decode(FileInfo file, uint height)
    {
        Interlocked.Increment(ref _inFlight);
        try
        {
            Calls.Enqueue($"decode:{file.Name}");
            DecodeGate?.Wait(TimeSpan.FromSeconds(10));
            return Bitmap(height);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public object? DecodeWithoutReadyMade(FileInfo file, uint height)
    {
        Interlocked.Increment(ref _inFlight);
        try
        {
            Calls.Enqueue($"decode-full:{file.Name}");
            DecodeFullGate?.Wait(TimeSpan.FromSeconds(10));
            return CanDecodeFull ? Bitmap(FullHeightCap is { } cap ? Math.Min(height, cap) : height) : null;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public long SizeInBytes(object thumbnail) =>
        thumbnail is Bitmap bitmap ? (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4 : 0;

    public object? ScaleDown(object image, uint height)
    {
        Calls.Enqueue("scale");
        return Bitmap(height);
    }

    private static WriteableBitmap Bitmap(uint height) =>
        new(new PixelSize((int)(height * 3 / 2), (int)height), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
}
