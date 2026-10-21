using System.Runtime.InteropServices;
using PicView.Avalonia.Printing;
using PicView.Core.Printing;

namespace PicView.Tests.Print;

/// <summary>
/// A page a <see cref="FakeSystemPrinters"/> was sent, copied before the Print Job disposes it.
/// </summary>
internal sealed record SentPage(
    string PrinterName,
    string PaperSize,
    Orientations Orientation,
    int Copies,
    string Title,
    int Width,
    int Height,
    int RowBytes,
    byte[] Pixels)
{
    /// <summary>
    /// The color at a point given as a fraction of the page's width and height.
    /// </summary>
    public (byte R, byte G, byte B) ColorAt(double x, double y)
    {
        var offset = (int)(y * Height) * RowBytes + (int)(x * Width) * 4;
        return (Pixels[offset + 2], Pixels[offset + 1], Pixels[offset]);
    }
}

/// <summary>
/// System printers that record what they are sent. Each can block, or throw, to show what the Print Job does then.
/// </summary>
internal sealed class FakeSystemPrinters : ISystemPrinters
{
    public string[] PrinterNames { get; init; } = ["Office printer"];

    public string? DefaultPrinterName { get; init; }

    public Dictionary<string, string[]> PaperSizes { get; init; } = new(StringComparer.Ordinal);

    public bool ThrowsOnListing { get; init; }

    /// <summary>
    /// Blocks listing the printers until it is set.
    /// </summary>
    public ManualResetEventSlim ListingGate { get; } = new(true);

    /// <summary>
    /// Holds each print until it is completed with whether the system took the job; completed with true at first.
    /// </summary>
    public TaskCompletionSource<bool> PrintResult { get; set; } = CompletedWith(true);

    public List<SentPage> Sent { get; } = [];

    public static TaskCompletionSource<bool> CompletedWith(bool result)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        completion.SetResult(result);
        return completion;
    }

    public string[] GetPrinterNames()
    {
        ListingGate.Wait(TimeSpan.FromSeconds(10));
        if (ThrowsOnListing)
        {
            throw new NotImplementedException();
        }

        return PrinterNames;
    }

    public string? GetDefaultPrinterName() => DefaultPrinterName;

    public string[] GetPaperSizes(string printerName) =>
        PaperSizes.TryGetValue(printerName, out var paperSizes) ? paperSizes : [];

    public async ValueTask<bool> PrintAsync(string printerName, PrintedPage page)
    {
        var pixels = new byte[page.RowBytes * page.PixelHeight];
        Marshal.Copy(page.Pixels, pixels, 0, pixels.Length);
        lock (Sent)
        {
            Sent.Add(new SentPage(printerName, page.PaperSize, page.Orientation, page.Copies, page.Title,
                page.PixelWidth, page.PixelHeight, page.RowBytes, pixels));
        }

        return await PrintResult.Task.ConfigureAwait(false);
    }
}
