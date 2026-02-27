using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.History;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class FileSaverHelper
{
    public static async ValueTask<bool> SaveCurrentFile(MainViewModel vm)
    {
        if (vm is null)
        {
            return false;
        }

        bool isSaved;
        if (vm.PicViewer.FileInfo is null)
        {
            isSaved = await SaveFileAs(vm).ConfigureAwait(false);
        }
        else
        {
            isSaved = await SaveFileAsync(vm.PicViewer.FileInfo.CurrentValue.FullName,
                vm.PicViewer.FileInfo.CurrentValue.FullName, vm).ConfigureAwait(false);
        }

        if (isSaved)
        {
            // Remove cached value so that rotation or likewise will be updated when navigating back
            NavigationManager.RemoveFromPreloader(vm.PicViewer.FileInfo.CurrentValue.FullName);
            await NavigationManager.QuickReload();
            await vm.HistoryManager.SetHasChanges(false);
        }

        // TODO: Add visual design to tell whether file was saved
        // TODO: Update thumbnail in gallery
        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAs(MainViewModel vm)
    {
        if (vm is null)
        {
            return false;
        }

        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.PicViewer?.FileInfo?.CurrentValue is null
            ? Path.GetRandomFileName()
            : vm.PicViewer.FileInfo.CurrentValue.Name;

        var isSaved = await FilePicker.PickAndSaveFileAsAsync(fileName, vm);
        if (isSaved)
        {
            NavigationManager.RemoveFromPreloader(fileName);
        }

        // TODO: Add visual design to tell whether file was saved
        // TODO: Update thumbnail in gallery
        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAsync(string? filename, string destination, MainViewModel vm)
    {
        if (vm.PicViewer.HasChanges.Value)
        {
            return await SaveImageFromBitmap();
        }

        if (vm.PicViewer.EffectConfig.Value is not null)
        {
            return await SaveImageFromBitmap();
        }

        if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
        {
            return await SaveImageFromFile();
        }

        return await SaveImageFromBitmap();

        async ValueTask<bool> SaveImageFromFile()
        {
            return await SaveImageFileHelper.SaveImageAsync(null,
                filename,
                destination,
                null,
                null,
                null,
                Path.GetExtension(destination),
                null,
                false,
                false,
                true);
        }

        async ValueTask<bool> SaveImageFromBitmap()
        {
            try
            {
                await using (DebouncedLoadingScope.Start(vm.MainWindow.IsLoadingIndicatorShown, 150))
                {
                    switch (vm.PicViewer.ImageType.CurrentValue)
                    {
                        case ImageType.AnimatedGif: // TODO: Add animated GIF support
                        case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                        case ImageType.Bitmap:
                        {
                            if (vm.PicViewer.ImageSource.CurrentValue is not Bitmap bitmap)
                            {
                                throw new InvalidOperationException("No bitmap available for saving.");
                            }

                            const uint quality = 100; // TODO: Add quality slider to user settings
                            var stream = new FileStream(destination, FileMode.Create);
                            bitmap.Save(stream, (int)quality);
                            await stream.DisposeAsync().ConfigureAwait(false);
                            var ext = Path.GetExtension(destination);

                            if (ext.IsSupported())
                            {
                                await SaveImageFileHelper.SaveImageAsync(
                                    null,
                                    destination,
                                    destination,
                                    null,
                                    null,
                                    quality,
                                    ext);
                            }

                            break;
                        }
                        case ImageType.Svg:
                            // TODO convert svg to bitmap and save
                            throw new InvalidOperationException("No bitmap available for saving.");
                        default:
                            throw new InvalidOperationException("No bitmap available for saving.");
                    }   
                }
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), ex);
                DialogManager.AddMessageDialog(TranslationManager.Translation.SavingFileFailed, ex.Message);
                return false;
            }

            return true;
        }
    }

    public static async Task ExportToPdf(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.PicViewer?.FileInfo?.CurrentValue is null ? Path.GetRandomFileName() : vm.PicViewer.FileInfo.CurrentValue.Name;

        await FilePicker.PickAndExportToPdfAsync(fileName, vm);
    }

    public static async Task ExportToPdfAsync(string? filename, string destination, MainViewModel vm)
    {
        try
        {
            if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
                return;
            
            var sourceMagick = (MagickImage)bmp.ToMagickImage();            

            // Ensure borderless export
            sourceMagick.Page = new MagickGeometry(sourceMagick.Width, sourceMagick.Height);
            sourceMagick.BackgroundColor = MagickColors.Transparent;
            sourceMagick.Density = new Density(300, 300);

            // Optional: flatten transparency for PDF rendering consistency
            if (sourceMagick.HasAlpha)
                sourceMagick.Alpha(AlphaOption.Remove);

            sourceMagick.Write(destination, MagickFormat.Pdf);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), ex);
        }
    }

    public static async Task<bool> PromptSaveChangesAsync() => await DialogManager.AddSaveDialog(TranslationManager.Translation.Save, TranslationManager.Translation.SaveChanges);

}