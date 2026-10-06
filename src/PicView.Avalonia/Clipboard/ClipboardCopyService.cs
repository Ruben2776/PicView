using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.IPlatform;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

public class ClipboardCopyService(MainWindow mainWindow) : ICopyClipboard
{
    public async Task<bool> CopyTextAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            _ = AnimationsHelper.CopyAnimation(mainWindow);
            await mainWindow.Clipboard.ClearAsync().ConfigureAwait(false);
            await mainWindow.Clipboard.SetTextAsync(text).ConfigureAwait(false);
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
        if (image is not Bitmap bitmap)
        {
            return false;
        }

        try
        {
            _ = AnimationsHelper.CopyAnimation(mainWindow);
            await mainWindow.Clipboard.ClearAsync().ConfigureAwait(false);
            await mainWindow.Clipboard.SetBitmapAsync(bitmap).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyImageAsync), ex);
            return false;
        }
    }

    public async Task<bool> CopyFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            var storageFile = await mainWindow.StorageProvider.TryGetFileFromPathAsync(Path.GetFullPath(filePath)).ConfigureAwait(false);
            if (storageFile is null)
            {
                return false;
            }

            var animTask = AnimationsHelper.CopyAnimation(mainWindow);
            var fileTask = mainWindow.Clipboard.SetFileAsync(storageFile);
            await Task.WhenAll(animTask, fileTask).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyFileAsync), ex);
        }
        return false;
    }

    public async Task<bool> CutFileAsync(string? filePath)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return false;
        }

        if (!await core.PlatformService.CutFile(filePath).ConfigureAwait(false))
        {
            return false;
        }

        await AnimationsHelper.CopyAnimation(mainWindow).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> CopyBase64Async(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            var base64 = Convert.ToBase64String(bytes);

            if (!string.IsNullOrEmpty(base64))
            {
                _ = AnimationsHelper.CopyAnimation(mainWindow);
                await mainWindow.Clipboard.ClearAsync().ConfigureAwait(false);
                await mainWindow.Clipboard.SetTextAsync(base64).ConfigureAwait(false);
                return true;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(CopyBase64Async), ex);
        }
        return false;
    }

    public async Task DuplicateFileAsync(string? sourcePath, string? currentActiveFilePath, MainWindowViewModel vm)
    {
        var targetPath = string.IsNullOrWhiteSpace(sourcePath) ? currentActiveFilePath : sourcePath;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }
        
        try
        {
            vm.IsLoadingIndicatorShown.Value = true;
            
            if (string.Equals(targetPath, currentActiveFilePath, StringComparison.Ordinal))
            {
                var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
                var duplicatedPath = await FileHelper.DuplicateAndReturnFileNameAsync(targetPath).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(duplicatedPath) && File.Exists(duplicatedPath))
                {
                    _ = AnimationsHelper.CopyAnimation(mainWindow);
                    await vm.WindowTabs.SharedNavigation.LoadFromFileAsync(duplicatedPath, activeTab, activeTab.GetTabCancellation()).ConfigureAwait(false);
                }
            }
            else
            {
                var duplicatedPath = await FileHelper.DuplicateAndReturnFileNameAsync(targetPath).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(duplicatedPath) && mainWindow != null)
                {
                    await AnimationsHelper.CopyAnimation(mainWindow).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardCopyService), nameof(DuplicateFileAsync), ex);
            TooltipHelper.ShowTooltipMessage(TranslationManager.Translation?.UnexpectedError);
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }
    }
}
