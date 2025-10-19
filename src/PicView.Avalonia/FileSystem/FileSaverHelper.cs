using Avalonia.Media.Imaging;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.FileSystem;

public static class FileSaverHelper
{
    public static async Task SaveCurrentFile(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        
        if (vm.PicViewer.FileInfo is null)
        {
            await SaveFileAs(vm);
        }
        else
        {
            await SaveFileAsync(vm.PicViewer.FileInfo.CurrentValue.FullName, vm.PicViewer.FileInfo.CurrentValue.FullName, vm);
        }
        
        //TODO: Add visual design to tell the user that file was saved
    }
    
    public static async Task SaveFileAs(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        
        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.PicViewer?.FileInfo?.CurrentValue is null ? Path.GetRandomFileName() : vm.PicViewer.FileInfo.CurrentValue.Name;

        await FilePicker.PickAndSaveFileAsAsync(fileName, vm);
    }

    public static async Task SaveFileAsync(string? filename, string destination, MainViewModel vm)
    {
        if (vm.PicViewer.HasChanges.Value)
        {
            await SaveImageFromBitmap();
        }
        else if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
        {
            await SaveImageFromFile();
        }
        else
        {
            await SaveImageFromBitmap();
        }
        
        return;

        async Task SaveImageFromFile()
        {
            await SaveImageFileHelper.SaveImageAsync(null,
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
        
        async Task SaveImageFromBitmap()
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
                                width: null,
                                height: null,
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
            }
        }
    }
}
