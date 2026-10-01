using Avalonia.Input.Platform;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Clipboard;

/// <summary>
/// Handles clipboard operations related to text
/// </summary>
public static class ClipboardTextOperations
{
    public static async Task<bool> CopyTextToClipboard(string text, MainWindow mainWindow)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return false;
        }

        try
        {
            _ = AnimationsHelper.CopyAnimation(mainWindow);
            await clipboard.ClearAsync().ConfigureAwait(false);
            await clipboard.SetTextAsync(text).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardTextOperations), nameof(CopyTextToClipboard), ex);
            return false;
        }
        return true;
    }
}