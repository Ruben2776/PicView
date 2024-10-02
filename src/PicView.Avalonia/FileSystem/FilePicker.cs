using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
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

        var file = await SelectFile();
        if (file is null)
        {
            return;
        }
        
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? file.Path.AbsolutePath : file.Path.LocalPath;
        await Task.Run(() => NavigationHelper.LoadPicFromStringAsync(path, vm));
    }
    
    public static async Task<IStorageFile?> SelectFile()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");
            var options = new FilePickerOpenOptions
            {
                Title = $"{TranslationHelper.Translation.OpenFileDialog} - PicView",
                AllowMultiple = false,
                FileTypeFilter = [AllFileType, FilePickerFileTypes.ImageAll, ArchiveFileType]
            };
            IReadOnlyList<IStorageFile> files;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                files  = await provider.OpenFilePickerAsync(new FilePickerOpenOptions());
            }
            else
            {
                files = await provider.OpenFilePickerAsync(options);
            }

            return files?.Count >= 1 ? files[0] : null;
        }
        catch (Exception e)
        {
            #if DEBUG
            Console.WriteLine(e);
            #endif
            await TooltipHelper.ShowTooltipMessageAsync(e);
        }

        return null;
    }

    private static FilePickerFileType AllFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles"))
    {
        Patterns = SupportedFiles.ConvertFilesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.image"],
        MimeTypes = ["image/*"],
    };

    private static FilePickerFileType ArchiveFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles"))
    {
        Patterns = SupportedFiles.ConvertArchivesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.archive"],
        MimeTypes = ["archive/*"]
    };

    public static async Task PickAndSaveFileAsAsync(string? fileName, MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        
        IStorageFile? file;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            file  = await provider.SaveFilePickerAsync(new FilePickerSaveOptions());
        }
        else
        {
            var options = new FilePickerSaveOptions
            {
                Title = $"{TranslationHelper.Translation.OpenFileDialog} - PicView",
                FileTypeChoices  = [AllFileType, FilePickerFileTypes.ImageAll, ArchiveFileType],
                SuggestedFileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetRandomFileName() : fileName,
                SuggestedStartLocation = await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(fileName)
            
            };
            file = await provider.SaveFilePickerAsync(options);
        }

        if (file is null)
        {
            // User exited
            return;
        }

        var destination = file.Path.LocalPath; // TODO: Handle macOS
        await FileSaverHelper.SaveFileAsync(fileName, destination, vm);
    }
}