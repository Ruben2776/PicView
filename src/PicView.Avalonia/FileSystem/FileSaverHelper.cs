using Avalonia;
using Avalonia.Media.Imaging;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileSaverHelper
{
    public static async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        bool isSaved;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.FileInfo is null)
        {
            isSaved = await SaveFileAs(vm).ConfigureAwait(false);
        }
        else
        {
            isSaved = await SaveFileAsync(tab.FileInfo.CurrentValue.FullName,
                tab.FileInfo.CurrentValue.FullName, vm).ConfigureAwait(false);
        }
        
        if (isSaved)
        {
            await tab.ImageIterator.ReloadAsync();
            // TODO: Add visual design to tell whether file was saved
        }
        
        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAs(MainWindowViewModel vm)
    {
        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue is null
            ? Path.GetRandomFileName()
            : vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.Name;
        
        var isSaved = await FilePicker.PickAndSaveFileAsAsync(fileName, vm);
        if (isSaved)
        {
            // TODO: Add visual design to tell whether file was saved
        }

        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAsync(string? filename, string destination, MainWindowViewModel vm)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return false;
        }
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var angle = tab.RotationAngle.CurrentValue;
        var isFlipped = tab.ScaleX.CurrentValue is -1;
        if (core.Effects?.ProcessedImage is { } magick)
        {
            return await SaveProcessedMagickImage();
        }
        
        if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
        {
            return await SaveImageFromFile();
        }
        
        return await SaveBitmap();
        
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
                isFlipped);
            ResetFlipIfNeeded();
            return isSaved;
        }
        
        async ValueTask<bool> SaveBitmap()
        {
            try
            {
                switch (vm.WindowTabs.ActiveTab.CurrentValue.ImageType.CurrentValue)
                {
                    case ImageType.AnimatedGif: // TODO: Add animated GIF support
                    case ImageType.AnimatedHeic: // TODO: Add animated HEIC support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                    case ImageType.Bitmap:
                    {
                        if (tab.Image.CurrentValue is not Bitmap bitmap)
                        {
                            return false;
                        }

                        await using var stream = FileStreamUtils.GetOptimizedFileStream(new FileInfo(filename), true);
                        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                        ResetFlipIfNeeded();
                        break;
                    }
                    case ImageType.Svg:
                        // TODO convert svg to bitmap and save
                        return await SaveImageFromFile();
                    default:
                        throw new InvalidOperationException("No bitmap available for saving.");
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), e);
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
                    case ImageType.AnimatedHeic: // TODO: Add animated HEIC support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
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
                        await magick.WriteAsync(destination);
                        ResetFlipIfNeeded();
                        break;
                    }
                    case ImageType.Svg:
                        // TODO convert svg to bitmap and save
                        return await SaveImageFromFile();
                    default:
                        throw new InvalidOperationException("No bitmap available for saving.");
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), e);
                return false;
            }
        
            return true;
        }
        
        void ResetFlipIfNeeded()
        {
            if (!isFlipped)
            {
                return;
            }
            // Revert flip after saving it (so that it does not flip the already flipped image again)
            tab.ScaleX.Value = 1;
        }
    }
}