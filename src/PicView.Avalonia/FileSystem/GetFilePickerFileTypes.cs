using Avalonia.Platform.Storage;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class GetFilePickerFileTypes
{
    public static FilePickerFileType AllFileType { get; } = new(TranslationManager.Translation.Image)
    {
        Patterns = SupportedFiles.ConvertFilesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.image"],
        MimeTypes = ["image/*"]
    };

    public static FilePickerFileType AvifFileType { get; } = new(".avif")
    {
        Patterns = ["*.avif"],
        AppleUniformTypeIdentifiers = ["public.avif"],
        MimeTypes = ["image/avif"]
    };

    public static FilePickerFileType TiffFileType { get; } = new(".tiff")
    {
        Patterns = ["*.tiff", "*.tif"],
        AppleUniformTypeIdentifiers = ["public.tiff"],
        MimeTypes = ["image/tiff"]
    };

    public static FilePickerFileType WebpFileType { get; } = new(".webp")
    {
        Patterns = ["*.webp"],
        AppleUniformTypeIdentifiers = ["org.webmproject.webp"],
        MimeTypes = ["image/webp"]
    };

    public static FilePickerFileType PngFileType { get; } = new(".png")
    {
        Patterns = ["*.png"],
        AppleUniformTypeIdentifiers = ["public.png"],
        MimeTypes = ["image/png"]
    };

    public static FilePickerFileType JpegFileType { get; } = new(".jpg")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.jfif"],
        AppleUniformTypeIdentifiers = ["public.jpeg"],
        MimeTypes = ["image/jpeg"]
    };

    public static FilePickerFileType ArchiveFileType { get; } = new(TranslationManager.GetTranslation("Archives"))
    {
        Patterns = SupportedFiles.ConvertArchivesToGlobFormat(),
        AppleUniformTypeIdentifiers = ["public.archive"],
        MimeTypes =
        [
            "application/zip", "application/x-rar-compressed", "application/x-tar", "application/x-7z-compressed"
        ]
    };

    public static FilePickerFileType GifFileType { get; } = new(".gif")
    {
        Patterns = ["*.gif"],
        AppleUniformTypeIdentifiers = ["com.compuserve.gif"],
        MimeTypes = ["image/gif"]
    };

    public static FilePickerFileType BmpFileType { get; } = new(".bmp")
    {
        Patterns = ["*.bmp"],
        AppleUniformTypeIdentifiers = ["com.microsoft.bmp"],
        MimeTypes = ["image/bmp"]
    };

    public static FilePickerFileType SvgFileType { get; } = new(".svg")
    {
        Patterns = ["*.svg"],
        AppleUniformTypeIdentifiers = ["public.svg-image"],
        MimeTypes = ["image/svg+xml"]
    };

    public static FilePickerFileType HeicFileType { get; } = new(".heic")
    {
        Patterns = ["*.heic"],
        AppleUniformTypeIdentifiers = ["public.heic"],
        MimeTypes = ["image/heic"]
    };

    public static FilePickerFileType HeifFileType { get; } = new(".heif")
    {
        Patterns = ["*.heif"],
        AppleUniformTypeIdentifiers = ["public.heif"],
        MimeTypes = ["image/heif"]
    };
}