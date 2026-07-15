using Avalonia;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileSaverHelper
{
    public static async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        bool isSaved;
        if (vm.WindowTabs.ActiveTab.CurrentValue.FileInfo is null)
        {
            isSaved = await SaveFileAs(vm).ConfigureAwait(false);
        }
        else
        {
            isSaved = await SaveFileAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName,
                vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName, vm).ConfigureAwait(false);
        }
        
        if (isSaved)
        {
            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
            var tab = vm.WindowTabs.ActiveTab.CurrentValue;
            var id = tab.Id;
            var index = tab.ImageIterator.CurrentIndex;
            var cache = core.SharedCache;
            var model = await GetImageModel.GetImageModelAsync(tab.FileInfo.CurrentValue);
            cache.UpdateImageModel(id, index, model);
            tab.Model = model;
            tab.UpdateTabTitle();
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
        if (core.Effects?.ProcessedImage is not null)
        {
            return await SaveProcessedImage();
        }
        
        if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
        {
            return await SaveImageFromFile();
        }
        
        return await SaveProcessedImage();
        
        async ValueTask<bool> SaveImageFromFile()
        {
            return await SaveImageFileHelper.SaveImageAsync(null,
                filename,
                destination,
                null,
                null,
                null,
                Path.GetExtension(destination),
                vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue,
                null,
                false,
                false,
                true,
                vm.WindowTabs.ActiveTab.CurrentValue.ScaleX.Value == -1);
        }
        
        async ValueTask<bool> SaveProcessedImage()
        {
            try
            {
                switch (vm.WindowTabs.ActiveTab.CurrentValue.ImageType.CurrentValue)
                {
                    case ImageType.AnimatedGif: // TODO: Add animated GIF support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                        return await SaveImageFromFile();
                    case ImageType.Bitmap:
                    {
                        if (core.Effects?.ProcessedImage is not MagickImage magick)
                        {
                            throw new InvalidOperationException("No bitmap available for saving.");
                        }
                        if (vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue is not 0)
                        {
                            magick.Rotate(vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue);
                        }
                        await magick.WriteAsync(destination);
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
    }
}