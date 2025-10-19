using Avalonia.Input.Platform;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Clipboard;

public static class ClipboardPasteOperations
{
    /// <summary>
    /// Pastes content from the clipboard
    /// </summary>
    /// <param name="vm">The main view model</param>
    public static async Task Paste(MainViewModel vm)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return;
        }

        try
        {
            // Need to use dispatcher to access clipboard in this instance
            var files = await Dispatcher.UIThread.InvokeAsync(async () => await clipboard.TryGetFilesAsync());
            if (files != null)
            {
                await ClipboardFileOperations.PasteFiles(files, vm);
                return;
            }

            // Try to paste text (URLs, file paths)
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                await NavigationManager.LoadPicFromStringAsync(text, vm).ConfigureAwait(false);
                return;
            }

            // Try to paste image data
            await ClipboardImageOperations.PasteClipboardImage(vm);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteOperations), nameof(Paste), ex);
        }
    }
}
