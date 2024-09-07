using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using ImageMagick;
using PicView.Avalonia.Navigation;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public static class ImageHelper
{
    #region Image Handling
    
    public static async Task<ImageModel?> GetImageModelAsync(FileInfo fileInfo, bool isThumb = false, uint height = 0)
    {
        return await Task.Run(async () =>
        {
            var imageModel = new ImageModel { FileInfo = fileInfo };

            try
            {
                var ext = fileInfo.Extension.ToLower();
                if (string.IsNullOrEmpty(ext))
                {
                    using var magickImage = new MagickImage();
                    magickImage.Ping(fileInfo);
                    ext = magickImage.Format.ToString();
                    var gif = ext.Equals("gif", StringComparison.InvariantCultureIgnoreCase);
                    var webp = ext.Equals("webp", StringComparison.InvariantCultureIgnoreCase);
                    if (!isThumb)
                    {
                        await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                        if (IsAnimated(fileInfo))
                        {
                            if (gif)
                            {
                                imageModel.ImageType = ImageType.AnimatedGif;
                            }
                            else if (webp)
                            {
                                imageModel.ImageType = ImageType.AnimatedWebp;
                            }
                        }
                        return imageModel;
                    }
                    
                    var svg = ext.Equals("svg", StringComparison.InvariantCultureIgnoreCase) || ext.Equals("svgz", StringComparison.InvariantCultureIgnoreCase);
                    if (svg)
                    {
                        AddSvgImage(fileInfo, imageModel);
                        return imageModel;
                    }

                    await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                    return imageModel;
                }
                switch (ext)
                {
                    case ".webp":
                        if (isThumb)
                        {
                            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                            if (IsAnimated(fileInfo))
                            {
                                imageModel.ImageType = ImageType.AnimatedWebp;
                            }
                        }
                        break;
                    case ".gif":
                        if (isThumb)
                        {
                            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                            if (IsAnimated(fileInfo))
                            {
                                imageModel.ImageType = ImageType.AnimatedGif;
                            }
                        }
                        break;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".bmp":
                    case ".jfif":
                    case ".ico":
                    case ".wbmp":
                        if (isThumb)
                        {
                            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                        }
                        break;

                    case ".svg":
                    case ".svgz":
                        AddSvgImage(fileInfo, imageModel);
                        break;

                    case ".b64":
                        await AddBase64ImageAsync(fileInfo, imageModel, isThumb, height).ConfigureAwait(false);
                        break;

                    default:
                        await AddDefaultImageAsync(fileInfo, imageModel, isThumb, height).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new ImageModel
                {
                    FileInfo = fileInfo,
                    ImageType = ImageType.Invalid,
                    Image = null, // TODO replace with error image
                    PixelHeight = 0,
                    PixelWidth = 0,
                    EXIFOrientation = EXIFHelper.EXIFOrientation.None
                };
            }

            return imageModel;
        });
    }
    


    private static async Task AddImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        const int bufferSize = 4096;
        await using var fs = new FileStream(
                                        fileInfo.FullName,
                                        FileMode.Open,
                                        FileAccess.Read,
                                        FileShare.ReadWrite,
                                        bufferSize,
                                        useAsync: fileInfo.Length > 1e7);
        Add(fs, imageModel);
    }

    private static async Task AddDefaultImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, uint height)
    {
        if (isThumb)
        {
            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
        }
        else
        {
            using var magickImage = new MagickImage();
            await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, true);
            if (imageModel.FileInfo.Length >= 2147483648)
            {
                // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                // ReSharper disable once MethodHasAsyncOverload
                magickImage.Read(fileStream);
            }
            else
            {
                await magickImage.ReadAsync(fileStream).ConfigureAwait(false);
            }
            magickImage.Format = MagickFormat.Png;
            await using var memoryStream = new MemoryStream();
            await magickImage.WriteAsync(memoryStream);
            memoryStream.Position = 0;
            Add(memoryStream, imageModel);
        }
    }


    
    #endregion
    
    #region Bitmap
    
    private static void Add(Stream stream, ImageModel imageModel)
    {
        var bitmap = new Bitmap(stream);
        imageModel.Image = bitmap;
        imageModel.PixelWidth = bitmap?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = bitmap?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }
    
    #endregion
    
    #region SVG
    
    private static void AddSvgImage(FileInfo fileInfo, ImageModel imageModel)
    {
        var svg = new MagickImage();
        svg.Ping(fileInfo.FullName);
        imageModel.PixelWidth = (int)svg.Width;
        imageModel.PixelHeight = (int)svg.Height;
        imageModel.ImageType = ImageType.Svg;
        imageModel.Image = fileInfo.FullName;
    }
    
    #endregion

    #region Base64

    private static async Task AddBase64ImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, uint height)
    {
        using var magickImage = await ImageDecoder.Base64ToMagickImage(fileInfo.FullName).ConfigureAwait(false);
        using var b64Stream = new MemoryStream();
        if (isThumb)
        {
            magickImage.Thumbnail(0, height);
        }
        else
        {
            await magickImage.WriteAsync(b64Stream);
            b64Stream.Position = 0;
        }
        Add(b64Stream, imageModel);
    }

    #endregion
    
    #region Thumbnail

    public static async Task<Bitmap?> GetThumbAsync(string path, uint height, FileInfo? fileInfo = null)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(path);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumbAsync(magick, path, height, fileInfo).ConfigureAwait(false);
            }
            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null)
            {
                return await CreateThumbAsync(magick, path, height, fileInfo).ConfigureAwait(false);
            }

            var byteArray = thumbnail.ToByteArray();
            var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private static async Task AddThumbAsync(FileInfo fileInfo, ImageModel imageModel, uint height)
    {
        var thumb = await GetThumbAsync(fileInfo.FullName, height, fileInfo).ConfigureAwait(false);
        imageModel.Image = thumb;
        imageModel.PixelWidth = thumb?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = thumb?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }

    private static async Task<Bitmap> CreateThumbAsync(IMagickImage magick, string path, uint height, FileInfo? fileInfo = null)
    {
        fileInfo ??= new FileInfo(path);
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, true);
        if (fileInfo.Length >= 2147483648)
        {
            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
            // ReSharper disable once MethodHasAsyncOverload
            magick.Read(fileStream);
        }
        else
        {
            await magick.ReadAsync(fileStream).ConfigureAwait(false);
        }

        var geometry = new MagickGeometry(0, height);
        magick.Thumbnail(geometry);
        magick.Format = MagickFormat.Png;
        await using var memoryStream = new MemoryStream();
        await magick.WriteAsync(memoryStream);
        memoryStream.Position = 0;
        return WriteableBitmap.Decode(memoryStream);
    }
    
    #endregion

    #region Helpers
    
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
    
    public static bool HasTransparentBackground(object imageSource)
    {
        // TODO implement
        return true;
    }
    
    #endregion
}