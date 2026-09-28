using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public class FileSavingService(FilePickerService? filePickerService = null)
{
    private readonly FilePickerService _filePickerService = filePickerService ?? new FilePickerService();

    public async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        bool isSaved;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        bool isCurrentImage;
        if (tab.FileInfo?.CurrentValue is null)
        {
            // If the viewed pic is not a file, open file picker
            isSaved = await SaveFileAs(vm).ConfigureAwait(false);
            isCurrentImage = false;
        }
        else
        {
            isSaved = await SaveFileAsync(tab.FileInfo.CurrentValue.FullName,
                tab.FileInfo.CurrentValue.FullName, vm).ConfigureAwait(false);
            isCurrentImage = true;
        }

        if (!isSaved)
        {
            return false;
        }

        if (!isCurrentImage)
        {
            return true;
        }

        tab.ImageIterator.Cache.DeleteFromCache(tab.FileInfo.CurrentValue.FullName);
        // TODO: Add visual design to tell whether file was saved

        return true;
    }

    public async ValueTask<bool> SaveFileAs(MainWindowViewModel vm)
    {
        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue is null
            ? Path.GetRandomFileName()
            : vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.Name;
        
        var isSaved = await _filePickerService.PickAndSaveFileAsAsync(fileName, vm).ConfigureAwait(false);
        if (isSaved)
        {
            // TODO: Add visual design to tell whether file was saved
        }

        return isSaved;
    }

    public async ValueTask<bool> SaveFileAsync(string? filename, string destination, MainWindowViewModel vm)
    { 
        var core = Dispatcher.UIThread.CheckAccess()
            ? Application.Current.DataContext as CoreViewModel
            : await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var angle = tab.RotationAngle.CurrentValue;
        var isFlipped = tab.ScaleX.CurrentValue is -1;
        if (core.Effects?.ProcessedImage is { } magick)
        {
            return await SaveProcessedMagickImage().ConfigureAwait(false);
        }
        
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return await SaveImageFromFile().ConfigureAwait(false);
        }
        
        return await SaveBitmap().ConfigureAwait(false);
        
        async ValueTask<bool> SaveImageFromFile()
        {
            var isSaved = await SaveImageFileHelper.SaveImageAsync(null,
                filename,
                destination,
                null,
                null,
                null,
                Path.GetExtension(destination),
                angle,
                null,
                false,
                false,
                true,
                isFlipped).ConfigureAwait(false);
            return isSaved;
        }
        
        async ValueTask<bool> SaveBitmap()
        {
            try
            {
                switch (vm.WindowTabs.ActiveTab.CurrentValue.ImageType.CurrentValue)
                {
                    case ImageType.AnimatedGif: // TODO: Add animated GIF support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                    case ImageType.AnimatedAvif: // TODO: Add animated AVIF support
                    case ImageType.MotionPhoto: // Saves the still cover; the embedded video is only kept via the file-copy path
                    case ImageType.Bitmap:
                    {
                        if (tab.Image.CurrentValue is not Bitmap bitmap)
                        {
                            return false;
                        }

                        if (string.IsNullOrWhiteSpace(filename)) return false;

                        var stream = FileStreamUtils.GetOptimizedFileStream(new FileInfo(filename), true);
                        await using (stream.ConfigureAwait(false))
                        {
                            bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                        }
                        break;
                    }
                    case ImageType.Svg:
                        // TODO convert svg to bitmap and save
                        return await SaveImageFromFile().ConfigureAwait(false);
                    default:
                        throw new InvalidOperationException("No bitmap available for saving.");
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSavingService), nameof(SaveFileAsync), e);
                return false;
            }
        
            return true;
        }
        
        async ValueTask<bool> SaveProcessedMagickImage()
        {
            try
            {
                switch (vm.WindowTabs.ActiveTab.CurrentValue.ImageType.CurrentValue)
                {
                    case ImageType.AnimatedGif: // TODO: Add animated GIF support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                    case ImageType.AnimatedAvif: // TODO: Add animated AVIF support
                    case ImageType.MotionPhoto: // Saving a processed image drops the embedded video, keep the still only
                    case ImageType.Bitmap:
                    {
                        if (angle is not 0)
                        {
                            magick.Rotate(angle);
                        }

                        if (isFlipped)
                        {
                            magick.Flop();
                        }
                        await magick.WriteAsync(destination).ConfigureAwait(false);
                        break;
                    }
                    case ImageType.Svg:
                        // TODO convert svg to bitmap and save
                        return await SaveImageFromFile().ConfigureAwait(false);
                    default:
                        throw new InvalidOperationException("No bitmap available for saving.");
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSavingService), nameof(SaveFileAsync), e);
                return false;
            }
        
            return true;
        }
    }
}
