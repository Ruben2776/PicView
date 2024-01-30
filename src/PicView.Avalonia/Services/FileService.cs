using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Avalonia.Services;

public class FileService
{
    public static async Task<IStorageFile?> OpenFile(CancellationToken token)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");
            var options = new FilePickerOpenOptions
            {
                Title = $"{TranslationHelper.GetTranslation("OpenFileDialog")} - PicView",
                AllowMultiple = false,
                FileTypeFilter = new[] { AllFileType, FilePickerFileTypes.ImageAll, ArchiveFileType }
            };
            var files = await provider.OpenFilePickerAsync(options);

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
        MimeTypes = new[] { "image/*" }
    };

    private static FilePickerFileType ArchiveFileType { get; } = new(TranslationHelper.GetTranslation("SupportedFiles"))
    {
        Patterns = SupportedFiles.ConvertArchivesToGlobFormat(),
        AppleUniformTypeIdentifiers = new[] { "public.archive" },
        MimeTypes = new[] { "archive/*" }
    };
}