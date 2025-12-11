using Avalonia.Media.Imaging;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

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
                vm.PicViewer.RotationAngle.CurrentValue,
                null,
                false,
                false,
                true,
                vm.PicViewer.ScaleX.Value == -1);
        }

        async ValueTask<bool> SaveImageFromBitmap()
        {
            try
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
                        // Add rotation, apply image conversion
                        if (ext.IsSupported())
                        {
                            await SaveImageFileHelper.SaveImageAsync(
                                null,
                                destination,
                                destination,
                                null,
                                null,
                                quality,
                                ext,
                                vm.PicViewer.RotationAngle.CurrentValue);
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
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), e);
                return false;
            }

            return true;
        }
    }
}