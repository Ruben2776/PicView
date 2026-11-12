using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ImageTransformations;
using PicView.Core.DebugTools;
using PicView.Core.ImageTransformations;
using PicView.Core.IPlatform;
using PicView.Core.Svg;
using PicView.Core.ViewModels;

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

#pragma warning disable MA0004
        // The clipboard and onCopied must be used on the UI thread, so a call from elsewhere, like after a file is
        // read, is moved there
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(() => CopyTextAsync(text));
        }

        try
        {
            // Continuations stay on the UI thread
            await clipboard.ClearAsync();
            await clipboard.SetTextAsync(text);
#pragma warning restore MA0004
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

#pragma warning disable MA0004
        // The clipboard and onCopied must be used on the UI thread, so a call from elsewhere, like after a decode, is
        // moved there
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(() => CopyImageAsync(bitmap));
        }

        try
        {
            // Continuations stay on the UI thread
            await clipboard.ClearAsync();
            await clipboard.SetBitmapAsync(bitmap);
#pragma warning restore MA0004
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

            // The read may have finished on a thread pool thread; CopyTextAsync moves to the UI thread
            return await CopyTextAsync(base64).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyBase64Async), ex);
        }
        return false;
    }

    public async Task<bool> CopyAxamlAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || clipboard is null || !IsSvgFile(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var svgText = await SvgFileReader.ReadAsync(path).ConfigureAwait(false);
            var axaml = SvgToXamlConverter.Convert(svgText, out _);
            // An unsupported SVG gives no text, which CopyTextAsync turns down without touching the clipboard. The
            // read may have finished on a thread pool thread; CopyTextAsync moves to the UI thread.
            return await CopyTextAsync(axaml).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyAxamlAsync), ex);
            return false;
        }
    }

    private static bool IsSvgFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".svgz", StringComparison.OrdinalIgnoreCase);
    }
}
