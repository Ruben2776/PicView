using Avalonia.Media.Imaging;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
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
        
        if (SettingsHelper.Settings.UIProperties.ShowFileSavingDialog)
        {
            if (vm.FileInfo is null)
            {
                await SaveFileAsync(null, vm.FileInfo.FullName, vm);
            }
            else
            {
                await FilePicker.PickAndSaveFileAsAsync(vm.FileInfo.FullName, vm);
            }
            
        }
        else
        {
            if (vm.FileInfo is null)
            {
                await SaveFileAsync(null, vm.FileInfo.FullName, vm);
            }
            else
            {
                await SaveFileAsync(vm.FileInfo.FullName, vm.FileInfo.FullName, vm);
            }
        }
    }

    public static async Task SaveFileAsync(string? filename, string destination, MainViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(filename))
        {
            await SaveImageFileHelper.SaveImageAsync(null,
                filename,
                destination,
                null,
                null,
                null,
                Path.GetExtension(destination),
                vm.RotationAngle);
        }
        else
        {
            switch (vm.ImageType)
            {
                case ImageType.AnimatedGif:
                case ImageType.AnimatedWebp:
                    throw new ArgumentOutOfRangeException();
                case ImageType.Bitmap:
                    if (vm.ImageSource is not Bitmap bitmap)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    var stream = new FileStream(destination, FileMode.Create);
                    const uint quality = 100;
                    bitmap.Save(stream, (int)quality);
                    await stream.DisposeAsync();
                    var ext = Path.GetExtension(destination);
                    if (ext != ".jpg" || ext != ".jpeg" || ext != ".png" || ext != ".bmp" || vm.RotationAngle != 0)
                    {
                        await SaveImageFileHelper.SaveImageAsync(
                            null,
                            destination,
                            destination:destination,
                            width: null,
                            height: null,
                            quality,
                            ext,
                            vm.RotationAngle);
                    }
                    
                    break;
                case ImageType.Svg:
                    throw new ArgumentOutOfRangeException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
