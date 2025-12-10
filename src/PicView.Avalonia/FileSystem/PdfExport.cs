using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class PdfExport
{
    public static async Task SavePdfWithFilePicker(MainViewModel? vm, RenderTargetBitmap bitmap)
    {
        vm ??= await Dispatcher.UIThread.InvokeAsync(() => UIHelper.GetMainView.DataContext as MainViewModel);
        var fileName = Path.GetFileNameWithoutExtension(vm.PicViewer.FileInfo?.Value?.Name)
                       ?? "export";

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return;
        }
        var options = new FilePickerSaveOptions
        {
            Title = TranslationManager.Translation.SaveAs + " - PicView",
            SuggestedFileName = fileName + ".pdf",
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
        bitmap.Save(pngStream);
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