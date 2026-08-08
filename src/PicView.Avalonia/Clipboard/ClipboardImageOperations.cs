using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

/// <summary>
///     Handles clipboard operations related to images
/// </summary>
public static class ClipboardImageOperations
{
    /// <summary>
    ///     Copies the current image to the clipboard
    /// </summary>
    public static async Task CopyImageToClipboard(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return;
        }
        if (vm.WindowTabs.ActiveTab.CurrentValue.CropService is CropService { IsCropping: true } cropService)
        {
            if (cropService.GetCroppedImage() is Bitmap clipboardBitmap)
            {
                await CopyImageToClipboard(clipboard, clipboardBitmap, mainWindow);
                return;
            }
        }
        if (vm.WindowTabs.ActiveTab.CurrentValue.Image.CurrentValue is not Bitmap bitmap)
        {
            return;
        }
        await CopyImageToClipboard(clipboard, bitmap, mainWindow);
    }
    
    public static async Task CopyImageToClipboard(IClipboard clipboard, Bitmap bitmap, MainWindow mainWindow)
    {
        _ = AnimationsHelper.CopyAnimation(mainWindow);
        await clipboard.ClearAsync();
        await clipboard.SetBitmapAsync(bitmap);
    }

    /// <summary>
    ///     Copies an image as base64 string to the clipboard
    /// </summary>
    public static async Task<bool> CopyBase64ToClipboard(string path, MainWindow mainWindow)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return false;
        }
        
        var base64 = await GetBase64String(path);
        
        if (string.IsNullOrEmpty(base64))
        {
            return false;
        }
        _ = AnimationsHelper.CopyAnimation(mainWindow);
        
        try
        {
            await clipboard.ClearAsync();
            await clipboard.SetTextAsync(base64);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardImageOperations), nameof(CopyBase64ToClipboard), ex);
            return false;
        }
    }

    private static async Task<string?> GetBase64String(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Convert.ToBase64String(await File.ReadAllBytesAsync(path));
        }
        return null; // TODO handle non-image types, such as SVGs
    }
    
    public static async Task PasteClipboardImage(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return;
        }

        try
        {
            var bitmap = await clipboard.TryGetBitmapAsync();
            if (bitmap is null)
            {
                return;
            }
            UpdateImage.SetSingleImage(vm, mainWindow, bitmap, SingleImageType.Clipboard, TranslationManager.Translation.ClipboardImage);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardImageOperations), nameof(PasteClipboardImage), ex);
        }
    }
    
    public static async Task CopyImageToClipboard(Bitmap bitmap)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return;
        }
        await clipboard.ClearAsync();
        await clipboard.SetBitmapAsync(bitmap);
    }
}