using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.Clipboard;

/// <summary>
/// Handles clipboard operations related to files
/// </summary>
public static class ClipboardFileOperations
{
    /// <summary>
    /// Duplicates the specified file, either the current file or another one specified by path.
    /// If the current file is being duplicated, the view model will navigate to the duplicated file.
    /// </summary>
    /// <param name="path">Path to the file to duplicate, or null to duplicate the current file.</param>
    /// <param name="vm">The main view model</param>
    public static async Task Duplicate(string path, MainViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        
        try
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = true;
            
            if (path == vm.PicViewer.FileInfo?.CurrentValue.FullName)
            {
                await DuplicateCurrentFile(vm);
            }
            else
            {
                await DuplicateFile(path);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardFileOperations), nameof(Duplicate), ex);
            TooltipHelper.ShowTooltipMessage(TranslationManager.Translation.UnexpectedError);
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    /// <summary>
    /// Duplicates the current file and navigates to it
    /// </summary>
    /// <param name="vm">The main view model</param>
    private static async Task DuplicateCurrentFile(MainViewModel vm)
    {
        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }

        if (Settings.Navigation.IsFileWatcherEnabled)
        {
            NavigationManager.ImageIterator.IsWatcherEnabled = false;
        }

        try
        {
            var duplicatedPath =
                await FileHelper.DuplicateAndReturnFileNameAsync(vm.PicViewer.FileInfo.CurrentValue.FullName);

            if (string.IsNullOrWhiteSpace(duplicatedPath) || !File.Exists(duplicatedPath))
            {
                return;
            }

            await NavigationManager.AddFile(duplicatedPath);
            await Task.WhenAll(
                AnimationsHelper.CopyAnimation(), 
                NavigationManager.LoadPicFromFile(duplicatedPath, vm)
            );
        }
        finally
        {
            if (Settings.Navigation.IsFileWatcherEnabled)
            {
                NavigationManager.EnableWatcher();
            }
            else
            {
                NavigationManager.DisableWatcher();
            }
        }
    }
    
    /// <summary>
    /// Duplicates the specified file and plays a copy animation when done. The original file is not navigated away from.
    /// </summary>
    /// <param name="path">Path to the file to duplicate</param>
    private static async Task DuplicateFile(string path)
    {
        var duplicatedPath = await FileHelper.DuplicateAndReturnFileNameAsync(path);
        if (!string.IsNullOrWhiteSpace(duplicatedPath))
        {
            await AnimationsHelper.CopyAnimation();
        }
    }

    /// <summary>
    /// Copies a file to the clipboard
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task CopyFileToClipboard(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } clipboard ||
            desktop.MainWindow?.StorageProvider is not { } storageProvider)
        {
            return;
        }
        
        var animTask = AnimationsHelper.CopyAnimation();
        var storageFile = await storageProvider.TryGetFileFromPathAsync(filePath);
        var fileTask = clipboard.SetFileAsync(storageFile);
        
        await Task.WhenAll(animTask, fileTask);
    }

    /// <summary>
    /// Cuts a file to the clipboard (copy + mark for deletion on paste)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="vm">The main view model</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static Task<bool> CutFile(string filePath, MainViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(false);
        }

        return ClipboardService.ExecuteClipboardOperation(
            () => Task.Run(() => vm.PlatformService.CutFile(filePath))
        );
    }
    
    /// <summary>
    /// Handles pasting files from the clipboard
    /// </summary>
    public static async Task PasteFiles(object files, MainViewModel vm)
    {
        try
        {
            switch (files)
            {
                case IEnumerable<IStorageItem> items:
                    await ProcessStorageItems(items.ToArray(), vm);
                    break;
                case IStorageItem singleFile:
                {
                    await NavigationManager.LoadPicFromStringAsync(singleFile.Path.AbsolutePath, vm);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardFileOperations), nameof(PasteFiles), ex);
        }
    }
    
    private static async Task ProcessStorageItems(IStorageItem[] storageItems, MainViewModel vm)
    {
        if (storageItems.Length == 0)
        {
            return;
        }

        // Load the first file
        await NavigationManager.LoadPicFromStringAsync(storageItems[0].Path.AbsolutePath, vm);

        // Open consecutive files in a new process
        foreach (var file in storageItems.Skip(1))
        {
            ProcessHelper.StartNewProcess(file.Path.AbsolutePath);
        }
    }
}