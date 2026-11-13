using System.Collections.Concurrent;
using PicView.Core.Thumbnailing;

namespace PicView.Tests.Thumbnailing;

/// <summary>
/// A thumbnail a <see cref="FakeThumbnailDecoder"/> made: where it came from and the height it was asked for.
/// </summary>
public sealed record FakeThumbnail(string Source, string Path, uint Height);

/// <summary>
/// Makes <see cref="FakeThumbnail"/>s without reading any file, and records what it was asked. By default every
/// file has a ready-made thumbnail and decodes, and any image scales down; each can be turned off.
/// </summary>
public sealed class FakeThumbnailDecoder : IThumbnailDecoder
{
    public bool HasReadyMade { get; set; } = true;
    public bool CanDecode { get; set; } = true;
    public bool CanScaleDown { get; set; } = true;

    /// <summary>
    /// When set, <see cref="Decode"/> waits for it before it returns, so a test can hold decodes in flight.
    /// </summary>
    public ManualResetEventSlim? DecodeGate { get; set; }

    public ConcurrentQueue<string> Calls { get; } = new();

    private int _decodesInFlight;
    private int _mostDecodesInFlight;

    /// <summary>The most decodes that ran at the same time.</summary>
    public int MostDecodesInFlight => Volatile.Read(ref _mostDecodesInFlight);

    public object? GetReadyMade(FileInfo file, uint height)
    {
        Calls.Enqueue($"ready:{file.Name}");
        return HasReadyMade ? new FakeThumbnail("ready", file.FullName, height) : null;
    }

    public object? Decode(FileInfo file, uint height)
    {
        Calls.Enqueue($"decode:{file.Name}");
        var inFlight = Interlocked.Increment(ref _decodesInFlight);
        int most;
        do
        {
            most = Volatile.Read(ref _mostDecodesInFlight);
        } while (inFlight > most && Interlocked.CompareExchange(ref _mostDecodesInFlight, inFlight, most) != most);

        try
        {
            DecodeGate?.Wait(TimeSpan.FromSeconds(10));
            return CanDecode ? new FakeThumbnail("decode", file.FullName, height) : null;
        }
        finally
        {
            Interlocked.Decrement(ref _decodesInFlight);
        }
    }

    public object? ScaleDown(object image, uint height)
    {
        Calls.Enqueue("scale");
        return CanScaleDown ? new FakeThumbnail("scale", image.ToString() ?? "", height) : null;
    }
}
