using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.Clipboard;
public static class ClipboardHelper
{
    private static async Task CopyAnimation()
    {
        const double speed = 0.2;
        const double opacity = 0.4;
        var startOpacityAnimation = AnimationsHelper.OpacityAnimation(0, opacity, speed);
        var endOpacityAnimation = AnimationsHelper.OpacityAnimation(opacity, 0, speed);
        Rectangle? rectangle = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            rectangle = new Rectangle
            {
                Width = UIHelper.GetMainView.Width,
                Height = UIHelper.GetMainView.Height,
                Opacity = 0,
                Fill = Brushes.Black,
                IsHitTestVisible = false
            };
            UIHelper.GetMainView.MainGrid.Children.Add(rectangle);
        });
        await startOpacityAnimation.RunAsync(rectangle);
        await endOpacityAnimation.RunAsync(rectangle);
        await Task.Delay(200);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UIHelper.GetMainView.MainGrid.Children.Remove(rectangle);
        });
    }
    public static async Task CopyTextToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await desktop.MainWindow.Clipboard.SetTextAsync(text);
        await CopyAnimation();
    }

    public static async Task CopyFileToClipboard(string? file, MainViewModel vm)
    {
        var success = await Task.Run(() => vm.PlatformService.CopyFile(file));
        if (success)
        {
            await CopyAnimation();
        }
    }

    public static async Task CopyImageToClipboard(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var clipboard = desktop.MainWindow.Clipboard;
        
        if (vm.ImageSource is not Bitmap bitmap)
        {
            return;
        }
        await clipboard.ClearAsync();
        
        // Handle for Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await Task.WhenAll(vm.PlatformService.CopyImageToClipboard(bitmap), CopyAnimation());
            return;
        }
        
        using var ms = new MemoryStream();
        bitmap.Save(ms);
                        
        var dataObject = new DataObject();
        dataObject.Set("image/png", ms.ToArray());
        await Task.WhenAll(clipboard.SetDataObjectAsync(dataObject), CopyAnimation());
    }

    public static async Task CopyBase64ToClipboard(string path, MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var clipboard = desktop.MainWindow.Clipboard;
        string base64;
        if (string.IsNullOrWhiteSpace(path))
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
                    var stream = new MemoryStream();
                    bitmap.Save(stream, quality: 100);
                    base64 = Convert.ToBase64String(stream.ToArray());
                    break;
                case ImageType.Svg:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(path));
        }

        if (string.IsNullOrEmpty(base64))
        {
            return;
        }

        await clipboard.SetTextAsync(base64);
        await CopyAnimation();
    }   

    public static async Task CutFile(string path, MainViewModel vm)
    {
        var success = await Task.Run(() => vm.PlatformService.CutFile(path));
        if (success)
        {
            await CopyAnimation();
        }
    }
    
    public static async Task Paste(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var clipboard = desktop.MainWindow.Clipboard;
        var files = await clipboard.GetDataAsync(DataFormats.Files);
        if (files is not null)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (files is IEnumerable<IStorageItem> items)
            {
                var storageItems = items.ToArray(); // Ensure we have an array for indexed access
                if (storageItems.Length > 0)
                {
                    // load the first file
                    var firstFile = storageItems[0];
                    var firstPath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
                    await NavigationHelper.LoadPicFromStringAsync(firstPath, vm).ConfigureAwait(false);

                    // Open consecutive files in a new process
                    foreach (var file in storageItems.Skip(1))
                    {
                        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? file.Path.AbsolutePath : file.Path.LocalPath;
                        ProcessHelper.StartNewProcess(path);
                    }
                }
            }
            else if (files is IStorageItem singleFile)
            {
                var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? singleFile.Path.AbsolutePath : singleFile.Path.LocalPath;
                await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
            }
            return;
        }
        
        var name = TranslationHelper.Translation.ClipboardImage;
        var imageType = ImageType.Bitmap;
        var bitmap = await GetBitmapFromBytes("PNG");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        
        bitmap = await GetBitmapFromBytes("image/jpeg");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/png");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/bmp");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("BMP");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("JPG");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("JPEG");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/tiff");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("GIF");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/gif");
        if (bitmap is not null)
        {
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        
        var text = await clipboard.GetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {   
            await NavigationHelper.LoadPicFromStringAsync(text, vm).ConfigureAwait(false);
            return;
        }
        // Handle for Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            bitmap = await vm.PlatformService.GetImageFromClipboard();
            UpdateImage.SetSingleImage(bitmap, imageType, name, vm);
            return;
        }
        return;
        
        async Task<Bitmap?> GetBitmapFromBytes(string format)
        {
            var data = await clipboard.GetDataAsync(format);
            if (data is byte[] dataBytes)
            {
                using var memoryStream = new MemoryStream(dataBytes);
                var image = new Bitmap(memoryStream);
                return image;
            }
        
            return null;
        }
    }

}
