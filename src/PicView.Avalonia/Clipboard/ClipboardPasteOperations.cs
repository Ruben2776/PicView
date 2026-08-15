using Avalonia;
using Avalonia.Input.Platform;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

public static class ClipboardPasteOperations
{
    /// <summary>
    /// Pastes content from the clipboard
    /// </summary>
    public static async ValueTask<bool> Paste(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return false;
        }
        
        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        tab.SetLoading();

        try
        {
            // Need to use dispatcher to access clipboard in this instance
            var files = await clipboard.TryGetFilesAsync();
            if (files != null)
            {
                await ClipboardFileOperations.ProcessStorageItems(files, vm, mainWindow).ConfigureAwait(false);
                return true;
            }
            
            if (Application.Current.DataContext is not CoreViewModel core)
            {
                return false;
            }

            // Try to paste text (URLs, file paths)
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (Base64Decoder.IsBase64String(text, out var base64))
                {
                    await UpdateImage.SetSingeBase64ImageAsync(base64, vm, mainWindow, tab.GetTabCancellation().Token);
                }
                
                if (tab.IsInitialized)
                {
                    return await tabs.LoadFromStringAsync(text);
                }
                
                await QuickLoad.QuickLoadAsync(mainWindow, core, text, false);
                return true;
            }

            // Try to paste image data
            await ClipboardImageOperations.PasteClipboardImage(vm, mainWindow);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteOperations), nameof(Paste), ex);
        }
        return false;
    }
}