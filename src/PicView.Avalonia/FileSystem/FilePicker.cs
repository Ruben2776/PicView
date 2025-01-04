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
        
        await Task.Run(() => NavigationHelper.LoadPicFromStringAsync(file, vm));
    }

    public static async Task<string?> SelectFile()
    {
        var file = await SelectIStorageFile();
        if (file is null)
        {
            return null;
        }
        
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? file.Path.AbsolutePath : file.Path.LocalPath;
    }
    
    public static async Task<IStorageFile?> SelectIStorageFile()
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
                FileTypeFilter = [
                    AllFileType,
                    FilePickerFileTypes.ImageAll,
                    JpegFileType,
                    PngFileType,
                    GifFileType,
                    BmpFileType,
                    WebpFileType,
                    TiffFileType,
                    AvifFileType,
                    HeicFileType,
                    HeifFileType,
                    SvgFileType,
                    ArchiveFileType]
            };
            IReadOnlyList<IStorageFile> files;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions());
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

    private static FilePickerFileType AllFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles")) // TODO: Get translation
    {
        Patterns = SupportedFiles.ConvertFilesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.image"],
        MimeTypes = ["image/*"],
    };
    
    private static FilePickerFileType AvifFileType { get; } = new(".avif")
    {
        Patterns = new List<string>{"*.avif"},
        AppleUniformTypeIdentifiers = ["public.image"], // TODO: Get AppleUniformTypeIdentifiers for avif
        MimeTypes = ["image/avif"],
    };
    
    private static FilePickerFileType TiffFileType { get; } = new(".tiff")
    {
        Patterns = new List<string>{"*.tiff", "*.tif"},
        AppleUniformTypeIdentifiers = ["public.tiff"],
        MimeTypes = ["image/tiff"],
    };
    
    private static FilePickerFileType WebpFileType { get; } = new(".webp")
    {
        Patterns = new List<string>{"*.webp"},
        AppleUniformTypeIdentifiers = ["org.webmproject.webp"],
        MimeTypes = ["image/webp"],
    };
    
    private static FilePickerFileType PngFileType { get; } = new(".png")
    {
        Patterns = new List<string>{"*.png"},
        AppleUniformTypeIdentifiers = ["public.png"],
        MimeTypes = ["image/png"],
    };
    
    private static FilePickerFileType JpegFileType { get; } = new(".jpg")
    {
        Patterns = new List<string>{"*.jpg","*.jpeg, *.jfif"},
        AppleUniformTypeIdentifiers = ["public.jpeg"],
        MimeTypes = ["image/jpeg"],
    };

    private static FilePickerFileType ArchiveFileType { get; } = new(TranslationHelper.GetTranslation("Archives")) // TODO: Get translation
    {
        Patterns = SupportedFiles.ConvertArchivesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.archive"],
        MimeTypes = ["archive/*"]
    };
    
    private static FilePickerFileType GifFileType { get; } = new(".gif")
    {
        Patterns = new List<string>{"*.gif"},
        AppleUniformTypeIdentifiers = ["com.compuserve.gif"],
        MimeTypes = ["image/gif"],
    };
    
    private static FilePickerFileType BmpFileType { get; } = new(".bmp")
    {
        Patterns = new List<string>{"*.bmp"},
        AppleUniformTypeIdentifiers = ["com.microsoft.bmp"],
        MimeTypes = ["image/bmp"],
    };
    
    private static FilePickerFileType SvgFileType { get; } = new(".svg")
    {
        Patterns = new List<string>{"*.svg"},
        AppleUniformTypeIdentifiers = ["public.svg-image"],
        MimeTypes = ["image/svg+xml"],
    };
    
    private static FilePickerFileType HeicFileType { get; } = new(".heic")
    {
        Patterns = new List<string>{"*.heic"},
        AppleUniformTypeIdentifiers = ["public.heic"],
        MimeTypes = ["image/heic"],
    };
    
    private static FilePickerFileType HeifFileType { get; } = new(".heif")
    {
        Patterns = new List<string>{"*.heif"},
        AppleUniformTypeIdentifiers = ["public.heif"],
        MimeTypes = ["image/heif"],
    };

    public static async Task PickAndSaveFileAsAsync(string? fileName, MainViewModel vm)
    {
        var file = await PickFileForSavingAsync(fileName);
        if (file is null)
        {
            return;
        }
        
        await FileSaverHelper.SaveFileAsync(fileName, file, vm);
    }
    
    public static async Task<string?> PickFileForSavingAsync(string? fileName)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var options = new FilePickerSaveOptions
        {
            Title = $"{TranslationHelper.Translation.OpenFileDialog} - PicView",
            FileTypeChoices  = [
                FilePickerFileTypes.ImageAll,
                JpegFileType,
                PngFileType,
                GifFileType,
                BmpFileType,
                WebpFileType,
                TiffFileType,
                AvifFileType,
                HeicFileType,
                HeifFileType,
                SvgFileType],
            SuggestedFileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetRandomFileName() : Path.GetFileName(fileName),
            SuggestedStartLocation = await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(fileName)
            
        };
        var file = await provider.SaveFilePickerAsync(options);
        var destination = file?.Path.LocalPath; // TODO: Handle macOS
        return destination;
    }

    public static async Task<string> SelectDirectory()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var options = new FolderPickerOpenOptions
        {
            Title = TranslationHelper.Translation.Folder + " - PicView",
            AllowMultiple = false
        };
        
        var directory = await provider.OpenFolderPickerAsync(options);
        if (directory is null)
        {
            return "";
        }
        if (directory.Count <= 0)
        {
            return "";
        }
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? directory[0].Path.AbsolutePath : directory[0].Path.LocalPath;
        
    }
}