using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public static class ImageFunctions
{
    public static void SetImage(object image, Image imageControl, ImageType imageType)
    {
        imageControl.Source = imageType switch
        {
            ImageType.Svg => new SvgImage { Source = SvgSource.Load(image as string) },
            ImageType.Bitmap => image as Bitmap,
            _ => imageControl.Source
        };
    }

    public static bool IsAnimated(FileInfo fileInfo)
    {
        var frames = ImageFunctionHelper.GetImageFrames(fileInfo.FullName);
        return frames > 1;
    }
    
    public static async Task<string> ConvertToCommonSupportedFormatAsync(string path)
    {
        var url = path.GetURL();
        if (!string.IsNullOrWhiteSpace(url))
        {
            // Download the image from the URL
            path = await DownloadImageFromUrlAsync(url);
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty; // If download fails, return empty
            }
        }
        
        if (path.StartsWith("file:///"))
        {
            path = path.Replace("file:///", "");
            path = path.Replace("%20", " ");
        }
        if (!File.Exists(path))
            return string.Empty;
        
        var ext = Path.GetExtension(path).ToLower();
        switch (ext)
        {
            case ".gif": // Don't know what to do if animated?
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".jpe":
            case ".bmp":
            case ".jfif":
                return path;

            default:
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ext);
                // Save image to temp path
                var success = await SaveImageFileHelper.SaveImageAsync(null, path, tempPath, null, null, null, ".png");
                return !success ? string.Empty : tempPath;
        }
    }
    
    
    private static async Task<string> DownloadImageFromUrlAsync(string url)
    {
        // TODO: Refactoring needed: Need to combine with the one in LoadPicFromUrlAsync and add unit tests
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(url));

            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                await using var fs = new FileStream(tempPath, FileMode.Create);
                await response.Content.CopyToAsync(fs);
                return tempPath;
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"Error downloading image: {ex.Message}");
#endif
            return string.Empty;
        }

        return string.Empty;
    }
}
