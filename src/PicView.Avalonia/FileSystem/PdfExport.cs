using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Printing;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Printing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class PdfExport
{
    public static async Task SavePdfWithFilePicker(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.Image.CurrentValue is not Bitmap avaloniaBmp)
        {
            return;
        }
        
        vm.PrintPreview ??= new PrintPreviewViewModel();
        PrintWindowConfig printWindowConfig;
        if (vm.PrintPreview.PrintWindowConfig is not null)
        {
            printWindowConfig = vm.PrintPreview.PrintWindowConfig;
        }
        else
        {
            printWindowConfig = new PrintWindowConfig();
            await printWindowConfig.LoadAsync();
        }
        
        var preview = vm.PrintPreview;
        var printSettings = preview.PrintSettings.Value ?? new PrintSettings();

        var settings = printWindowConfig.WindowProperties;
        var requestedName = settings.PaperSize ?? "A4";
        var (w, h) = PaperSizeHelper.GetMmSize(requestedName);
        var landscape = settings.Orientation is (int)Orientations.Landscape;
        var paperInfo = landscape ? new PaperInfo(requestedName, h, w)
            : new PaperInfo(requestedName, w, h);
        
        var layout = PrintCore.ComputeLayout(
            paperInfo.WidthMm, paperInfo.HeightMm, printSettings,
            avaloniaBmp.PixelSize.Width, avaloniaBmp.PixelSize.Height, 300f);
            
        var pageSize = new PixelSize((int)Math.Round(layout.PageWidthPx), (int)Math.Round(layout.PageHeightPx));
        var suggestedFileName = vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.Name + ".pdf";
        var rtb = new RenderTargetBitmap(pageSize);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var ctx = rtb.CreateDrawingContext();
            ctx.FillRectangle(Brushes.White, new Rect(0, 0, layout.PageWidthPx, layout.PageHeightPx));
            var dest = new Rect(layout.DrawX, layout.DrawY, layout.DrawWidth, layout.DrawHeight);
            ctx.DrawImage(avaloniaBmp, new Rect(0, 0, avaloniaBmp.PixelSize.Width, avaloniaBmp.PixelSize.Height), dest);
        });
        await SavePdfWithFilePicker(suggestedFileName, rtb);
    }
    
    public static async Task SavePdfWithFilePicker(string outputFilename, RenderTargetBitmap bitmap)
    {
        if (string.IsNullOrWhiteSpace(outputFilename))
        {
            throw new ArgumentException("Output filename cannot be null or whitespace", nameof(outputFilename));
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return;
        }
        var options = new FilePickerSaveOptions
        {
            Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.SaveAs),
            SuggestedFileName = outputFilename,
            FileTypeChoices =
            [
                new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }
            ]
        };
        
        var chosenFile = await Dispatcher.UIThread.InvokeAsync(() => provider.SaveFilePickerAsync(options));
        if (chosenFile is null)
        {
            return;
        }
        
        var localPath = chosenFile.Path.LocalPath;

        using var pngStream = new MemoryStream();
        // Avalonia's Bitmap.Save writes PNG by default (no format parameter).
        bitmap.Save(pngStream, PngBitmapEncoderOptions.Default);
        pngStream.Position = 0;

        // Use MagickImage (or MagickImageCollection for multi-page)
        using var image = new MagickImage(pngStream);

        // Density controls the resolution that the PDF will claim; set before writing.
        image.Density = new Density(300, 300); // 300 DPI 
        image.Quality = 100;                    // image quality
        image.Page = new MagickGeometry(image.Width, image.Height);
        // If you want a white background instead of transparency:
        if (image.HasAlpha)
        {
            // Flatten over white to avoid transparent page in some PDF viewers
            using var background = new MagickImage(MagickColors.White, image.Width, image.Height);
            background.Composite(image, CompositeOperator.Over);
            await background.WriteAsync(localPath, MagickFormat.Pdf).ConfigureAwait(false);
        }
        else
        {
            // Write PDF directly
            await image.WriteAsync(localPath, MagickFormat.Pdf).ConfigureAwait(false);
        }
    }
}