using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.Services;

public static class FilePickerHelper
{
    public static async Task<IStorageFile?> OpenFile()
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
                FileTypeFilter = new[] { AllFileType, FilePickerFileTypes.ImageAll, ArchiveFileType }
            };
            IReadOnlyList<IStorageFile> files;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                files  = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                });
            }
            else
            {
                files = await provider.OpenFilePickerAsync(options);
            }

            return files?.Count >= 1 ? files[0] : null;
        }
        catch (Exception e)
        {
            // TODO write exception service to display error messages
        }

        return null;
    }

    private static FilePickerFileType AllFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles"))
    {
        Patterns = SupportedFiles.ConvertFilesToGlobFormat(),
        AppleUniformTypeIdentifiers = new[] { "public.image" },
        MimeTypes = new[] { "image/*" },
    };

    private static FilePickerFileType ArchiveFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles"))
    {
        Patterns = SupportedFiles.ConvertArchivesToGlobFormat(),
        AppleUniformTypeIdentifiers = new[] { "public.archive" },
        MimeTypes = new[] { "archive/*" }
    };

    public static async Task SaveFileAsync(string? fileName, MainViewModel vm)
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
                FileTypeChoices  = new[] { AllFileType, FilePickerFileTypes.ImageAll, ArchiveFileType },
                SuggestedFileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetRandomFileName() : fileName,
                SuggestedStartLocation = await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(fileName)
            
            };
            file = await provider.SaveFilePickerAsync(options);
        }

        var path = file.Path.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            await SaveImageFileHelper.SaveImageAsync(null,
                fileName,
                path,
                null,
                null,
                null,
                Path.GetExtension(path),
                vm.RotationAngle);
        }
        else
        {
            switch (vm.ImageType)
            {
                case ImageType.AnimatedBitmap:
                    throw new ArgumentOutOfRangeException();
                case ImageType.Bitmap:
                    if (vm.ImageSource is not Bitmap bitmap)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    var stream = new FileStream(path, FileMode.Create);
                    var quality = 100;
                    bitmap.Save(stream, quality);
                    await stream.DisposeAsync();
                    var ext = Path.GetExtension(path);
                    if (ext is not ".jpg" or ".jpeg" or ".png" or ".bmp" || vm.RotationAngle != 0)
                    {
                        await SaveImageFileHelper.SaveImageAsync(
                            null,
                            path,
                            destination:path,
                            width: null,
                            height: null,
                            quality,
                            ext,
                            vm.RotationAngle);
                    }
                    
                    break;
                case ImageType.Svg:
                    throw new ArgumentOutOfRangeException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}