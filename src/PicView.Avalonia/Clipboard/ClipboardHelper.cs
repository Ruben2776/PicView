using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.Clipboard;
public static class ClipboardHelper
{
    public static async Task CopyTextToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await desktop.MainWindow.Clipboard.SetTextAsync(text);
    }

    public static async Task CopyFileToClipboard(string? file)
    {            
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Files, new[] { file });
        await desktop.MainWindow.Clipboard.SetDataObjectAsync(dataObject);
        // Doesn't work, TODO figure out how to add a file to the clipboard
    }

    public static async Task CopyImageToClipboard(string path)
    {
        throw new NotImplementedException();
    }

    public static async Task CopyBase64ToClipboard(string path)
    {
        throw new NotImplementedException();
    }   

    public static async Task CutFile(string path)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var clipboard = desktop.MainWindow.Clipboard;
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Files, new[] { path });
        await clipboard.ClearAsync();
        await clipboard.SetDataObjectAsync(dataObject);
    }
    
    public static async Task Paste(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var clipboard = desktop.MainWindow.Clipboard;
        var text = await clipboard.GetTextAsync();
        if (text is not null)
        {   
            var check = ErrorHelper.CheckIfLoadableString(text);
            switch (check)
            {
                case "":
                    break;
                default:
                    await NavigationHelper.LoadPicFromFile(check, vm).ConfigureAwait(false);
                    return;

                case "web":
                    await NavigationHelper.LoadPicFromUrlAsync(text, vm).ConfigureAwait(false);
                    return;

                case "base64":
                    await NavigationHelper.LoadPicFromBase64Async(text, vm).ConfigureAwait(false);
                    return;

                case "directory":
                    await NavigationHelper.LoadPicFromDirectoryAsync(text, vm).ConfigureAwait(false);
                    return;
            }
        }

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
        
        var bitmap = await GetBitmapFromBytes("PNG");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        
        bitmap = await GetBitmapFromBytes("image/jpeg");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/png");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/bmp");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("BMP");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("JPG");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("JPEG");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/tiff");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("GIF");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
            return;
        }
        bitmap = await GetBitmapFromBytes("image/gif");
        if (bitmap is not null)
        {
            NavigationHelper.SetSingleImage(bitmap, name, vm);
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
