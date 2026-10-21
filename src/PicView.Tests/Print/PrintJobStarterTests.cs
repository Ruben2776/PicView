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
public class PrintJobStarterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly MainWindowViewModel _vm;
    private readonly FakeSystemPrinters _printers = new() { PrinterNames = ["Office printer", "Photo printer"] };
    private readonly List<MainWindowViewModel> _shownPrintWindows = [];

    public PrintJobStarterTests()
    {
        SetDefaults();
        _vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        Directory.CreateDirectory(_tempDir);
        ShowRedImage();
    }

    public void Dispose()
    {
        SetDefaults();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private void ShowRedImage()
    {
        var bitmap = new WriteableBitmap(new PixelSize(40, 20), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using (var framebuffer = bitmap.Lock())
        {
            var red = new byte[framebuffer.RowBytes * 20];
            for (var i = 0; i < red.Length; i += 4)
            {
                red[i + 2] = 255;
                red[i + 3] = 255;
            }

            Marshal.Copy(red, 0, framebuffer.Address, red.Length);
        }

        var tab = new TabViewModel(_vm);
        tab.DisplaySingleImage(new ImageModel
        {
            Image = bitmap, PixelWidth = 40, PixelHeight = 20, ImageType = ImageType.Bitmap
        }, SingleImageType.Clipboard);
        tab.TabTitle.Value = "Clipboard image";
        _vm.WindowTabs.ActiveTab.Value = tab;
    }

    private PrintJobStarter CreateStarter(PrintWindowProperties remembered, StubSavePathPicker? pdfPathPicker = null) =>
        new(_printers, () => pdfPathPicker ?? new StubSavePathPicker(null),
            () => Task.FromResult<PrintWindowProperties?>(remembered),
            vm =>
            {
                _shownPrintWindows.Add(vm);
                return Task.CompletedTask;
            });

    private static void RunWithDispatcher(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted)
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        task.GetAwaiter().GetResult();
    }

    [AvaloniaFact]
    public void PrintAsync_PreviewOn_OpensPrintWindow()
    {
        Settings.UIProperties.ShowPrintPreview = true;

        RunWithDispatcher(CreateStarter(new PrintWindowProperties()).PrintAsync(_vm));

        Assert.Equal([_vm], _shownPrintWindows);
        Assert.Empty(_printers.Sent);
    }

    [AvaloniaFact]
    public void PrintAsync_PreviewOff_SendsToRememberedPrinterWithRememberedSettings()
    {
        Settings.UIProperties.ShowPrintPreview = false;
        var remembered = new PrintWindowProperties
        {
            PrinterName = "Photo printer", PaperSize = "A4", Copies = 2, MarginLeft = 50, ScaleMode = (int)ScaleModes.Fill
        };

        RunWithDispatcher(CreateStarter(remembered).PrintAsync(_vm));

        Assert.Empty(_shownPrintWindows);
        var sent = Assert.Single(_printers.Sent);
        Assert.Equal(("Photo printer", "A4", 2), (sent.PrinterName, sent.PaperSize, sent.Copies));
        // Fill covers the area inside the margins: the 50 mm left margin of a 210 mm page stays white
        Assert.Equal((255, 255, 255), sent.ColorAt(0.2, 0.5));
        Assert.Equal((255, 0, 0), sent.ColorAt(0.3, 0.5));
    }

    [AvaloniaFact]
    public void SaveAsPdfAsync_WritesPdfWithRememberedPaperWithoutPrintWindow()
    {
        var destination = Path.Combine(_tempDir, "saved.pdf");
        var pathPicker = new StubSavePathPicker(destination);
        var remembered = new PrintWindowProperties { PrinterName = "Office printer", PaperSize = "Letter" };

        RunWithDispatcher(CreateStarter(remembered, pathPicker).SaveAsPdfAsync(_vm));

        Assert.Empty(_shownPrintWindows);
        Assert.Empty(_printers.Sent);
        Assert.Equal("Clipboard image.pdf", pathPicker.SuggestedName);
        // Letter is 8.5 by 11 inches: 612 by 792 points
        Assert.Contains("612 792", File.ReadAllText(destination), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void PrintAndSaveAsPdf_SideBySide_DoNothing()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.ShowPrintPreview = true;
        var pathPicker = new StubSavePathPicker(Path.Combine(_tempDir, "saved.pdf"));
        var starter = CreateStarter(new PrintWindowProperties(), pathPicker);

        RunWithDispatcher(starter.PrintAsync(_vm));
        RunWithDispatcher(starter.SaveAsPdfAsync(_vm));
        Settings.UIProperties.ShowPrintPreview = false;
        RunWithDispatcher(starter.PrintAsync(_vm));

        Assert.Empty(_shownPrintWindows);
        Assert.Empty(_printers.Sent);
        Assert.Null(pathPicker.SuggestedName);
    }

    [AvaloniaFact]
    public void PrintAsync_TabShowsNoImage_DoesNothing()
    {
        Settings.UIProperties.ShowPrintPreview = true;
        _vm.WindowTabs.ActiveTab.Value = new TabViewModel(_vm);

        RunWithDispatcher(CreateStarter(new PrintWindowProperties()).PrintAsync(_vm));

        Assert.Empty(_shownPrintWindows);
    }
}
