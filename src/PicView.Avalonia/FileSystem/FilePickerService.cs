using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PicView.Avalonia.FileSystem;

public class FilePickerService(IStorageProvider? storageProvider = null)
{
    public async Task SelectAndLoadFile(MainWindow mainWindow, MainWindowViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        var file = await SelectFile().ConfigureAwait(false);
        if (file is null)
        {
            return;
        }
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current?.DataContext as CoreViewModel);
        if (core is not null)
        {
            UIHelper.CloseMenus(core);
        }

        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (!tab.IsInitialized)
        {
            if (core is not null)
            {
                await QuickLoad.QuickLoadAsync(mainWindow, core, file, continueFromLeftOff: false).ConfigureAwait(false);
            }
            return;
        }
        
        Dispatcher.UIThread.Invoke(() =>
        {
            if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not StartUpMenu)
            {
                return;
            }

            vm.WindowTabs.ActiveTab.Value.CurrentView.Value = new ImageViewer();
        });


        if (!tab.Gallery.IsGalleryDocked.CurrentValue)
        {
            vm.IsLoadingIndicatorShown.Value = true;
        }
        try
        {
            await vm.WindowTabs.LoadFromFileAsync(file).ConfigureAwait(false);
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }
    }

    public async Task<string?> SelectFile()
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var file = await SelectIStorageFile().ConfigureAwait(false);
            return file?.Path.LocalPath;
        });
    }

    private async Task<IStorageFile?> SelectIStorageFile()
    {
        try
        {
            var provider = GetStorageProvider();
            if (provider is null) return null;
            
            var options = new FilePickerOpenOptions
            {
                Title = $"{TranslationManager.Translation.OpenFileDialog} - PicView",
                AllowMultiple = false,
                FileTypeFilter = [
                    GetFilePickerFileTypes.AllFileType,
                    FilePickerFileTypes.ImageAll,
                    GetFilePickerFileTypes.JpegFileType,
                    GetFilePickerFileTypes.PngFileType,
                    GetFilePickerFileTypes.GifFileType,
                    GetFilePickerFileTypes.BmpFileType,
                    GetFilePickerFileTypes.WebpFileType,
                    GetFilePickerFileTypes.TiffFileType,
                    GetFilePickerFileTypes.AvifFileType,
                    GetFilePickerFileTypes.HeicFileType,
                    GetFilePickerFileTypes.HeifFileType,
                    GetFilePickerFileTypes.SvgFileType,
                    GetFilePickerFileTypes.ArchiveFileType]
            };

            var files = await ExecuteOnUIThread(() => provider.OpenFilePickerAsync(options)).ConfigureAwait(false);
            return files?.Count >= 1 ? files[0] : null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FilePickerService), nameof(SelectIStorageFile), e);
        }

        return null;
    }

    public async ValueTask<bool> PickAndSaveFileAsAsync(string? fileName, MainWindowViewModel vm)
    {
        var file = await PickFileForSavingAsync(fileName).ConfigureAwait(false);
        if (file is null)
        {
            return false;
        }

        return await FileSaver.SaveFileAsync(fileName, file, vm).ConfigureAwait(false);
    }
    
    public async Task<string?> PickFileForSavingAsync(string? fileName, string? ext = null)
    {
        try
        {
            var provider = GetStorageProvider();
            if (provider is null)
            {
                return null;
            }
        
            var suggestedFileName = GetSuggestedFileName(fileName, ext);

            var options = new FilePickerSaveOptions
            {
                Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.SaveAs),
                FileTypeChoices = [
                    FilePickerFileTypes.ImageAll,
                    GetFilePickerFileTypes.JpegFileType,
                    GetFilePickerFileTypes.PngFileType,
                    GetFilePickerFileTypes.GifFileType,
                    GetFilePickerFileTypes.BmpFileType,
                    GetFilePickerFileTypes.WebpFileType,
                    GetFilePickerFileTypes.TiffFileType,
                    GetFilePickerFileTypes.AvifFileType,
                    GetFilePickerFileTypes.HeicFileType,
                    GetFilePickerFileTypes.HeifFileType,
                    GetFilePickerFileTypes.SvgFileType],
                SuggestedFileName = suggestedFileName,
                SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(fileName ?? string.Empty).ConfigureAwait(false)
            };
            
            var file = await ExecuteOnUIThread(() => provider.SaveFilePickerAsync(options)).ConfigureAwait(false);
            return file?.Path.LocalPath;
        }
        catch (Exception e)
        {
            #if DEBUG
            Console.WriteLine(e);
            #endif
            return null;
        }
    }

    public async Task<string> SelectDirectory()
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var provider = GetStorageProvider();
            if (provider is null) return string.Empty;
    
            var options = new FolderPickerOpenOptions
            {
                Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.Folder),
                AllowMultiple = false
            };
            
            var directories = await ExecuteOnUIThread(() => provider.OpenFolderPickerAsync(options));
            
            if (directories is null || directories.Count <= 0)
            {
                return string.Empty;
            }
            
            return directories[0].Path.LocalPath;
        });
    }
    
    private IStorageProvider? GetStorageProvider()
    {
        if (storageProvider is not null)
        {
            return storageProvider;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.StorageProvider: { } provider })
        {
            return provider;
        }
#if DEBUG
        DebugHelper.LogDebug(nameof(FilePickerService), nameof(GetStorageProvider), "Missing StorageProvider instance.");
#endif
        return null;

    }
    
    private static string GetSuggestedFileName(string? fileName, string? ext)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Path.GetRandomFileName();
        }
        
        return string.IsNullOrWhiteSpace(ext) 
            ? Path.GetFileName(fileName) 
            : Path.GetFileName(Path.ChangeExtension(fileName, ext));
    }
    
    private static async Task<T> ExecuteOnUIThread<T>(Func<Task<T>> action)
    {
        // Try to use file picker in Dispatcher #228
        return await Dispatcher.UIThread.InvokeAsync(action).ConfigureAwait(false);
    }
}
