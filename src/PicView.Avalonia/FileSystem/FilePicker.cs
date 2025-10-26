using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class FilePicker
{
    public static async Task SelectAndLoadFile(MainViewModel vm)
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

        MenuManager.CloseMenus(vm);
        await NavigationManager.LoadPicFromStringAsync(file, vm).ConfigureAwait(false);
    }

    public static async Task<string?> SelectFile()
    {
        var file = await SelectIStorageFile().ConfigureAwait(false);
        return file?.Path.LocalPath;
    }

    private static async Task<IStorageFile?> SelectIStorageFile()
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
            DebugHelper.LogDebug(nameof(FilePicker), nameof(SelectIStorageFile), e);
        }

        return null;
    }

    public static async Task PickAndSaveFileAsAsync(string? fileName, MainViewModel vm)
    {
        var file = await PickFileForSavingAsync(fileName).ConfigureAwait(false);
        if (file is null)
        {
            return;
        }
        
        await FileSaverHelper.SaveFileAsync(fileName, file, vm).ConfigureAwait(false);
    }
    
    public static async Task<string?> PickFileForSavingAsync(string? fileName, string? ext = null)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
            {
                return null;
            }
        
            var suggestedFileName = GetSuggestedFileName(fileName, ext);

            var options = new FilePickerSaveOptions
            {
                Title = $"{TranslationManager.Translation.SaveAs} - PicView",
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
                SuggestedStartLocation = await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(fileName).ConfigureAwait(false)
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

    public static async Task<string> SelectDirectory()
    {
        var provider = GetStorageProvider();
        if (provider is null) return string.Empty;

        var options = new FolderPickerOpenOptions
        {
            Title = TranslationManager.Translation.Folder + " - PicView",
            AllowMultiple = false
        };
        
        var directories = await ExecuteOnUIThread(() => provider.OpenFolderPickerAsync(options)).ConfigureAwait(false);
        
        if (directories is null || directories.Count <= 0)
        {
            return string.Empty;
        }
        
        return directories[0].Path.LocalPath;
    }
    
    // Helper methods to reduce code duplication
    
    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.StorageProvider: { } provider })
        {
            return provider;
        }
#if DEBUG
        Console.WriteLine("Missing StorageProvider instance.");
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