using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PicView.Avalonia.ImageTransformations;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Avalonia.Clipboard;

public class ClipboardCopyService(
    IClipboard? clipboard,
    IStorageProvider? storageProvider = null,
    IPlatformSpecificService? platformService = null,
    Action? onCopied = null) : ICopyClipboard
{
    public async Task<bool> CopyTextAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || clipboard is null)
        {
            return false;
        }

        try
        {
            await clipboard.ClearAsync().ConfigureAwait(false);
            await clipboard.SetTextAsync(text).ConfigureAwait(false);
            onCopied?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyTextAsync), ex);
            return false;
        }
    }

    public async Task<bool> CopyImageAsync(object? image)
    {
        if (image is not Bitmap bitmap || clipboard is null)
        {
            return false;
        }

        try
        {
            await clipboard.ClearAsync().ConfigureAwait(false);
            await clipboard.SetBitmapAsync(bitmap).ConfigureAwait(false);
            onCopied?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyImageAsync), ex);
            return false;
        }
    }

    public Task<bool> CopyImageAsync(TabViewModel tab)
    {
        if (tab.Image.CurrentValue is not Bitmap bitmap)
        {
            return Task.FromResult(false);
        }

        var orientation = tab.Orientation.Current.CurrentValue;
        // A turned copy is never disposed here, since pasting in PicView can show that same bitmap in a tab
        return CopyImageAsync(orientation == Orientation.Upright ? bitmap : OrientedBitmap.Render(bitmap, orientation));
    }

    public async Task<bool> CopyFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || clipboard is null || storageProvider is null)
        {
            return false;
        }

        try
        {
            var storageFile = await storageProvider.TryGetFileFromPathAsync(Path.GetFullPath(filePath)).ConfigureAwait(false);
            if (storageFile is null)
            {
                return false;
            }

            await clipboard.SetFileAsync(storageFile).ConfigureAwait(false);
            onCopied?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyFileAsync), ex);
            return false;
        }
    }

    public async Task<bool> CutFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || platformService is null)
        {
            return false;
        }

        try
        {
            var result = await platformService.CutFile(filePath).ConfigureAwait(false);
            if (result)
            {
                onCopied?.Invoke();
            }
            return result;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CutFileAsync), ex);
            return false;
        }
    }

    public async Task<bool> CopyBase64Async(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || clipboard is null)
        {
            return false;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            var base64 = Convert.ToBase64String(bytes);

            if (!string.IsNullOrEmpty(base64))
            {
                await clipboard.ClearAsync().ConfigureAwait(false);
                await clipboard.SetTextAsync(base64).ConfigureAwait(false);
                onCopied?.Invoke();
                return true;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyBase64Async), ex);
        }
        return false;
    }
}
