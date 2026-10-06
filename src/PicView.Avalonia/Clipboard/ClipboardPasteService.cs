using Avalonia.Input.Platform;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

public class ClipboardPasteService(
    IClipboard? clipboard,
    MainWindow? mainWindow = null,
    CoreViewModel? core = null) : IPasteClipboard
{
    public async ValueTask<bool> PasteAsync(MainWindowViewModel vm)
    {
        if (clipboard is null)
        {
            return false;
        }

        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        tab.SetLoading();

        try
        {
            // 1. Try to paste files
            var files = await clipboard.TryGetFilesAsync().ConfigureAwait(false);
            if (files is { Length: > 0 })
            {
                var paths = new List<string>(files.Length);
                foreach (var file in files)
                {
                    paths.Add(file.Path.LocalPath);
                    file.Dispose();
                }

                return await PasteFilesAsync(paths, vm).ConfigureAwait(false);
            }

            // 2. Try to paste text (URLs, file paths, base64)
            var text = await clipboard.TryGetTextAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (await PasteTextAsync(text, vm).ConfigureAwait(false))
                {
                    return true;
                }
            }

            // 3. Try to paste image data
            return await PasteImageAsync(vm).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteService), nameof(PasteAsync), ex);
            return false;
        }
    }

    public async ValueTask<bool> PasteFilesAsync(IReadOnlyList<string> filePaths, MainWindowViewModel vm)
    {
        if (filePaths.Count is 0 || mainWindow is null)
        {
            return false;
        }

        var activeCore = core ?? vm.Core;
        if (activeCore is null)
        {
            return false;
        }

        var firstItem = filePaths[0];

        if (!vm.WindowTabs.ActiveTab.Value.IsInitialized)
        {
            await QuickLoad.QuickLoadAsync(mainWindow, activeCore, firstItem, continueFromLeftOff: false).ConfigureAwait(false);
        }
        else
        {
            if (firstItem.IsArchive())
            {
                await vm.WindowTabs.LoadFromArchiveAsync(firstItem).ConfigureAwait(false);
            }
            else
            {
                await vm.WindowTabs.LoadFromFileAsync(firstItem).ConfigureAwait(false);
            }
        }

        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked.CurrentValue)
        {
            return true;
        }

        // Open consecutive files in new tabs
        foreach (var path in filePaths.Skip(1))
        {
            var fileInfo = new FileInfo(path);
            var tab = vm.WindowTabs.CreateTab(fileInfo);
            TabNavigationInitializer.InitializeConsecutiveTab(activeCore, mainWindow, tab);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                tab.CurrentView.Value = new ImageViewer();
            });

            if (fileInfo.IsArchive())
            {
                _ = Task.Run(() => vm.WindowTabs.LoadFromArchiveAsync(path, tab).ConfigureAwait(false));
            }
            else
            {
                _ = Task.Run(() => vm.WindowTabs.LoadFromFileAsync(fileInfo, tab).ConfigureAwait(false));
            }
        }

        return true;
    }

    public async ValueTask<bool> PasteTextAsync(string text, MainWindowViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(text) || mainWindow == null)
        {
            return false;
        }

        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        if (Base64Decoder.IsBase64String(text, out var base64))
        {
            await UpdateImage.SetSingeBase64ImageAsync(base64, vm, mainWindow, tab.GetTabCancellation().Token).ConfigureAwait(false);
            return true;
        }

        if (tab.IsInitialized)
        {
            return await vm.WindowTabs.LoadFromStringAsync(text).ConfigureAwait(false);
        }

        var activeCore = core ?? vm.Core;
        if (activeCore is null)
        {
            return false;
        }

        await QuickLoad.QuickLoadAsync(mainWindow, activeCore, text, false).ConfigureAwait(false);
        return true;
    }

    public async ValueTask<bool> PasteImageAsync(MainWindowViewModel vm)
    {
        if (clipboard is null || mainWindow is null)
        {
            return false;
        }

        try
        {
            var bitmap = await clipboard.TryGetBitmapAsync().ConfigureAwait(false);
            if (bitmap is null)
            {
                return false;
            }

            UpdateImage.SetSingleImage(vm, mainWindow, bitmap, SingleImageType.Clipboard, TranslationManager.Translation?.ClipboardImage);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteService), nameof(PasteImageAsync), ex);
            return false;
        }
    }
}
