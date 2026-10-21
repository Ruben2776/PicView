using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PicView.Avalonia.Printing;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Printing;
using PicView.Core.ViewModels;
using PicView.Tests.FileTests;
using PrintWindowProperties = PicView.Core.Config.PrintWindowConfig.PrintWindowProperties;

namespace PicView.Tests.Print;

[Collection("Sequential")]
public class PrintJobTests : IDisposable
{
    private static readonly (byte R, byte G, byte B) White = (255, 255, 255);
    private static readonly (byte R, byte G, byte B) Red = (255, 0, 0);
    private static readonly (byte R, byte G, byte B) Blue = (0, 0, 255);
    private static readonly (byte R, byte G, byte B) Green = (0, 255, 0);

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly MainWindowViewModel _vm;
    private readonly PrintPreviewViewModel _preview = new();
    private PrintJob? _job;
    private int _closeCount;

    public PrintJobTests()
    {
        SetDefaults();
        _vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _job?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// The tab shows a 200 by 100 image, red on its left half and blue on its right.
    /// </summary>
    private TabViewModel ShowRedBlueImage()
    {
        var tab = new TabViewModel(_vm);
        tab.DisplaySingleImage(new ImageModel
        {
            Image = CreateBitmap(200, 100, x => x < 100 ? Red : Blue),
            PixelWidth = 200,
            PixelHeight = 100,
            ImageType = ImageType.Bitmap
        }, SingleImageType.Clipboard);
        tab.TabTitle.Value = "Clipboard image";
        _vm.WindowTabs.ActiveTab.Value = tab;
        return tab;
    }

    private static WriteableBitmap CreateBitmap(int width, int height, Func<int, (byte R, byte G, byte B)> colorOfColumn)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using var framebuffer = bitmap.Lock();
        var row = new byte[framebuffer.RowBytes];
        for (var x = 0; x < width; x++)
        {
            var (r, g, b) = colorOfColumn(x);
            row[x * 4] = b;
            row[x * 4 + 1] = g;
            row[x * 4 + 2] = r;
            row[x * 4 + 3] = 255;
        }

        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(row, 0, framebuffer.Address + y * framebuffer.RowBytes, row.Length);
        }

        return bitmap;
    }

    private static PrintWindowProperties A4Fit() => new()
    {
        PrinterName = "Office printer",
        PaperSize = "A4",
        ScaleMode = (int)ScaleModes.Fit,
        MarginTop = 10,
        MarginBottom = 10,
        MarginLeft = 10,
        MarginRight = 10
    };

    private PrintJob Open(FakeSystemPrinters printers, PrintWindowProperties? remembered = null,
        StubSavePathPicker? pdfPathPicker = null)
    {
        _job = PrintJob.Open(_vm, _preview, printers, pdfPathPicker ?? new StubSavePathPicker(null),
            remembered ?? A4Fit(), () => _closeCount++);
        return _job;
    }

    private static void RunWithDispatcher(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted)
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    private static T RunWithDispatcher<T>(Task<T> task)
    {
        RunWithDispatcher((Task)task);
        return task.Result;
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
    }

    private (byte R, byte G, byte B) PreviewColorAt(double x, double y)
    {
        var bitmap = Assert.IsType<WriteableBitmap>(_preview.PreviewImage.Value);
        using var framebuffer = bitmap.Lock();
        var offset = (int)(y * framebuffer.Size.Height) * framebuffer.RowBytes + (int)(x * framebuffer.Size.Width) * 4;
        var pixel = new byte[3];
        Marshal.Copy(framebuffer.Address + offset, pixel, 0, 3);
        return (pixel[2], pixel[1], pixel[0]);
    }

    private static bool IsNear((byte R, byte G, byte B) expected, (byte R, byte G, byte B) actual) =>
        Math.Abs(expected.R - actual.R) <= 8 && Math.Abs(expected.G - actual.G) <= 8 &&
        Math.Abs(expected.B - actual.B) <= 8;

    private static void AssertColor((byte R, byte G, byte B) expected, (byte R, byte G, byte B) actual)
    {
        Assert.InRange(actual.R, Math.Max(0, expected.R - 8), Math.Min(255, expected.R + 8));
        Assert.InRange(actual.G, Math.Max(0, expected.G - 8), Math.Min(255, expected.G + 8));
        Assert.InRange(actual.B, Math.Max(0, expected.B - 8), Math.Min(255, expected.B + 8));
    }

    [AvaloniaFact]
    public void Open_ShowsPreviewOfImageOnPaperAndHidesSpinner()
    {
        ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters { PaperSizes = { ["Office printer"] = ["A4", "Letter"] } });

        RunWithDispatcher(job.Loaded);

        Assert.False(_preview.IsProcessing.Value);
        Assert.Equal(["A4", "Letter"], _preview.PaperSizes.Value);
        Assert.Equal("Office printer", _preview.PrintSettings.Value.Printer.Value!.Id);
        // A4 at 96 DPI
        Assert.Equal(794, _preview.PageWidth.Value);
        Assert.Equal(1123, _preview.PageHeight.Value);
        AssertColor(White, PreviewColorAt(0.5, 0.2));
        AssertColor(Red, PreviewColorAt(0.3, 0.5));
        AssertColor(Blue, PreviewColorAt(0.7, 0.5));
    }

    [AvaloniaFact]
    public void Open_SlowPrinter_ReturnsAtOnceWithSpinnerShown()
    {
        ShowRedBlueImage();
        var printers = new FakeSystemPrinters();
        printers.ListingGate.Reset();

        var job = Open(printers);

        Assert.False(job.Loaded.IsCompleted);
        Assert.True(_preview.IsProcessing.Value);
        printers.ListingGate.Set();
        RunWithDispatcher(job.Loaded);
        Assert.False(_preview.IsProcessing.Value);
    }

    [AvaloniaFact]
    public void Open_PrintersCannotBeListed_OffersOnlySaveAsPdfAndHidesSpinner()
    {
        ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters { ThrowsOnListing = true });

        RunWithDispatcher(job.Loaded);

        var printer = Assert.Single(_preview.Printers.Value);
        Assert.True(printer.IsSaveAsPdf);
        Assert.Equal(PaperSizeHelper.GetAllNames(), _preview.PaperSizes.Value);
        Assert.False(_preview.IsProcessing.Value);
    }

    [AvaloniaFact]
    public void SendAsync_SendsThePagePreviewShows()
    {
        ShowRedBlueImage();
        var printers = new FakeSystemPrinters();
        var job = Open(printers);
        RunWithDispatcher(job.Loaded);

        var outcome = RunWithDispatcher(job.SendAsync());

        Assert.Equal(PrintOutcome.Sent, outcome);
        var sent = Assert.Single(printers.Sent);
        // A4 at 300 DPI
        Assert.Equal((2480, 3508), (sent.Width, sent.Height));
        Assert.Equal(("Office printer", "A4", "Clipboard image"), (sent.PrinterName, sent.PaperSize, sent.Title));
        for (var y = 0.05; y < 1; y += 0.1)
        {
            for (var x = 0.05; x < 1; x += 0.1)
            {
                AssertColor(PreviewColorAt(x, y), sent.ColorAt(x, y));
            }
        }
    }

    [AvaloniaFact]
    public void SendAsync_BlackAndWhite_PrintsGrayAndFollowsNextImage()
    {
        var tab = ShowRedBlueImage();
        var printers = new FakeSystemPrinters();
        var job = Open(printers);
        RunWithDispatcher(job.Loaded);

        _preview.PrintSettings.Value.ColorMode.Value = (int)ColorModes.BlackAndWhite;
        WaitUntil(() => PreviewColorAt(0.3, 0.5).G > 0);
        AssertColor((54, 54, 54), PreviewColorAt(0.3, 0.5));

        // The next image, or an Effects Render, replaces the bitmap the tab shows
        tab.Image.Value = CreateBitmap(200, 100, _ => Green);
        WaitUntil(() => PreviewColorAt(0.3, 0.5).G > 100);
        RunWithDispatcher(job.SendAsync());

        AssertColor((182, 182, 182), PreviewColorAt(0.3, 0.5));
        AssertColor((182, 182, 182), Assert.Single(printers.Sent).ColorAt(0.3, 0.5));
    }

    [AvaloniaFact]
    public void Preview_TurnedTab_ShowsImageTurned()
    {
        var tab = ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters());
        RunWithDispatcher(job.Loaded);

        // A quarter turn clockwise puts the left half on top
        tab.Orientation.TurnTo(1);
        WaitUntil(() => IsNear(Red, PreviewColorAt(0.5, 0.3)));

        AssertColor(Red, PreviewColorAt(0.5, 0.3));
        AssertColor(Blue, PreviewColorAt(0.5, 0.7));
    }

    [AvaloniaFact]
    public void Preview_FlippedTab_ShowsImageMirrored()
    {
        var tab = ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters());
        RunWithDispatcher(job.Loaded);

        tab.Orientation.Flip();
        WaitUntil(() => IsNear(Blue, PreviewColorAt(0.3, 0.5)));

        AssertColor(Blue, PreviewColorAt(0.3, 0.5));
        AssertColor(Red, PreviewColorAt(0.7, 0.5));
    }

    [AvaloniaFact]
    public void SendAsync_AfterTabSwitch_SendsTheOtherTab()
    {
        ShowRedBlueImage();
        var printers = new FakeSystemPrinters();
        var job = Open(printers);
        RunWithDispatcher(job.Loaded);

        var greenTab = new TabViewModel(_vm);
        greenTab.DisplaySingleImage(new ImageModel
        {
            Image = CreateBitmap(200, 100, _ => Green),
            PixelWidth = 200,
            PixelHeight = 100,
            ImageType = ImageType.Bitmap
        }, SingleImageType.Clipboard);
        _vm.WindowTabs.ActiveTab.Value = greenTab;
        WaitUntil(() => IsNear(Green, PreviewColorAt(0.3, 0.5)));
        RunWithDispatcher(job.SendAsync());

        AssertColor(Green, Assert.Single(printers.Sent).ColorAt(0.3, 0.5));
    }

    [AvaloniaFact]
    public void ChangingPrinter_OffersItsPaperAndKeepsChosenPaperWhenItHasIt()
    {
        ShowRedBlueImage();
        var printers = new FakeSystemPrinters
        {
            PrinterNames = ["Office printer", "Photo printer", "Poster printer"],
            PaperSizes =
            {
                ["Office printer"] = ["A4", "Letter"],
                ["Photo printer"] = ["Letter", "4x6"],
                ["Poster printer"] = ["A3"]
            }
        };
        var remembered = A4Fit();
        remembered.PaperSize = "Letter";
        var job = Open(printers, remembered);
        RunWithDispatcher(job.Loaded);
        var settings = _preview.PrintSettings.Value;

        settings.Printer.Value = _preview.Printers.Value.Single(p => p.Id == "Photo printer");
        WaitUntil(() => _preview.PaperSizes.Value.SequenceEqual(["Letter", "4x6"]));
        Assert.Equal("Letter", settings.PaperSize.Value);

        settings.Printer.Value = _preview.Printers.Value.Single(p => p.Id == "Poster printer");
        WaitUntil(() => _preview.PaperSizes.Value.SequenceEqual(["A3"]));
        Assert.Equal("A3", settings.PaperSize.Value);
    }

    [AvaloniaFact]
    public void SendAsync_ShowsSpinnerWhileSendingAndClosesWindowOnceSent()
    {
        ShowRedBlueImage();
        var printers = new FakeSystemPrinters
        {
            PrintResult = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var job = Open(printers);
        RunWithDispatcher(job.Loaded);

        var sending = job.SendAsync();
        WaitUntil(() => printers.Sent.Count == 1);

        Assert.True(_preview.IsProcessing.Value);
        printers.PrintResult.SetResult(true);
        Assert.Equal(PrintOutcome.Sent, RunWithDispatcher(sending));
        Assert.Equal(1, _closeCount);
    }

    [AvaloniaFact]
    public void SendAsync_PrinterRefuses_KeepsWindowOpenAndHidesSpinner()
    {
        ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters { PrintResult = FakeSystemPrinters.CompletedWith(false) });
        RunWithDispatcher(job.Loaded);

        var outcome = RunWithDispatcher(job.SendAsync());

        Assert.Equal(PrintOutcome.Failed, outcome);
        Assert.Equal(0, _closeCount);
        Assert.False(_preview.IsProcessing.Value);
    }

    [AvaloniaFact]
    public void SendAsync_SaveAsPdfCancelled_KeepsWindowOpen()
    {
        ShowRedBlueImage();
        var remembered = A4Fit();
        remembered.PrinterName = Printer.SaveAsPdfId;
        var pathPicker = new StubSavePathPicker(null);
        var job = Open(new FakeSystemPrinters(), remembered, pathPicker);
        RunWithDispatcher(job.Loaded);

        var outcome = RunWithDispatcher(job.SendAsync());

        Assert.Equal(PrintOutcome.Cancelled, outcome);
        Assert.Equal("Clipboard image.pdf", pathPicker.SuggestedName);
        Assert.Equal(0, _closeCount);
        Assert.False(_preview.IsProcessing.Value);
    }

    [AvaloniaFact]
    public void SendAsync_SaveAsPdf_WritesPdfOfPaperSizeAndCloses()
    {
        ShowRedBlueImage();
        var remembered = A4Fit();
        remembered.PrinterName = Printer.SaveAsPdfId;
        remembered.PaperSize = "Letter";
        var destination = Path.Combine(_tempDir, "printed.pdf");
        var job = Open(new FakeSystemPrinters(), remembered, new StubSavePathPicker(destination));
        RunWithDispatcher(job.Loaded);

        var outcome = RunWithDispatcher(job.SendAsync());

        Assert.Equal(PrintOutcome.Sent, outcome);
        Assert.Equal(1, _closeCount);
        // Letter is 8.5 by 11 inches: 612 by 792 points
        var pdf = File.ReadAllText(destination);
        Assert.Contains("612 792", pdf, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Cancel_ClosesWindow()
    {
        ShowRedBlueImage();
        var job = Open(new FakeSystemPrinters());
        RunWithDispatcher(job.Loaded);

        _preview.CancelCommand.Execute(R3.Unit.Default);
        WaitUntil(() => _closeCount == 1);
    }
}
