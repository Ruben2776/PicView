using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;

namespace PicView.Avalonia.ImageHandling;

public static class ImageHelper
{
    public static async Task<ImageModel?> GetImageModelAsync(FileInfo fileInfo, bool isThumb = false, int height = 0)
    {
        var imageModel = new ImageModel
        {
            FileInfo = fileInfo
        };
        try
        {
            switch (fileInfo.Extension.ToLower())
            {
                case ".gif":
                case ".png":
                case ".webp":
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".bmp":
                case ".jfif":
                case ".ico":
                case ".wbmp":
                {
                    if (isThumb)
                    {
                        await AddThumb().ConfigureAwait(false);
                        return imageModel;
                    }
                    var bytes = await FileHelper.GetBytesFromFile(fileInfo).ConfigureAwait(false);
                    using var memoryStream = new MemoryStream(bytes);
                    Add(memoryStream);
                    return imageModel;
                }

                case ".svg":
                case ".svgz":
                    imageModel.Image = fileInfo.FullName;
                    var svg = new MagickImage();
                    svg.Ping(fileInfo.FullName);
                    imageModel.PixelWidth = svg.Width;
                    imageModel.PixelHeight = svg.Height;
                    imageModel.ImageType = ImageType.Svg;
                    return imageModel;

                case ".b64":
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
                    Add(b64Stream);
                    return imageModel;
                }

                default:
                {
                    if (isThumb)
                    {
                        await AddThumb().ConfigureAwait(false);
                        return imageModel;
                    }
                    using var magickImage = new MagickImage();
                    await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite, 4096, true);
                    if (imageModel.FileInfo.Length >= 2147483648)
                    {
                        await Task.Run(() =>
                        {
                            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                            // ReSharper disable once MethodHasAsyncOverload
                            magickImage.Read(fileStream);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        await magickImage.ReadAsync(fileStream).ConfigureAwait(false);
                    }
                    magickImage.Format = MagickFormat.Png;
                    await using var memoryStream = new MemoryStream();
                    await magickImage.WriteAsync(memoryStream);
                    memoryStream.Position = 0;
                    Add(memoryStream);
                }
                    return imageModel;

                    void Add(Stream stream)
                    {
                        var bitmap = new Bitmap(stream);
                        imageModel.Image = bitmap;
                        imageModel.PixelWidth = bitmap?.PixelSize.Width ?? 0;
                        imageModel.PixelHeight = bitmap?.PixelSize.Height ?? 0;
                        imageModel.ImageType = ImageType.Bitmap;
                    }

                    async Task AddThumb()
                    {
                        var thumb = await GetThumb(fileInfo.FullName, height).ConfigureAwait(false);
                        imageModel.Image = thumb;
                        imageModel.PixelWidth = thumb?.PixelSize.Width ?? 0;
                        imageModel.PixelHeight = thumb?.PixelSize.Height ?? 0;
                        imageModel.ImageType = ImageType.Bitmap;
                    }
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
    }
    
    private static async Task<Bitmap?> GetThumb(string path, int height)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(path);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumb(magick).ConfigureAwait(false);
            }
            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null)
            {
                return await CreateThumb(magick).ConfigureAwait(false);
            }

            var byteArray = thumbnail.ToByteArray();
            var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
        async Task<Bitmap> CreateThumb(IMagickImage magick)
        {
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, true);
            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
            // ReSharper disable once MethodHasAsyncOverload
            magick.Read(fileStream);

            var geometry = new MagickGeometry(0, height);
            magick.Thumbnail(geometry);
            magick.Format = MagickFormat.Png;
            await using var memoryStream = new MemoryStream();
            await magick.WriteAsync(memoryStream);
            memoryStream.Position = 0;
            return WriteableBitmap.Decode(memoryStream);
        }
    }
    
    public static void SetImage(object image, Image imageControl, ImageType imageType)
    {
        imageControl.Source = imageType switch
        {
            ImageType.Svg => new SvgImage { Source = SvgSource.Load(image as string, null) },
            ImageType.Bitmap => image as Bitmap,
            ImageType.AnimatedBitmap => image as Bitmap,
            _ => imageControl.Source
        };
    }

    public static void SetClipboardImage(Bitmap bitmap, MainViewModel vm)
    {
        vm.ImageIterator = null;
        vm.ImageSource = bitmap;
        vm.ImageType = ImageType.Bitmap;
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        var name = TranslationHelper.Translation.ClipboardImage;
        if (GalleryFunctions.IsBottomGalleryOpen)
        {
            UIHelper.GetMainView.GalleryView.GalleryMode = GalleryMode.BottomToClosed;
        }
        WindowHelper.SetSize(width, height, vm);
        var titleString = TitleHelper.TitleString(width, height, name, 1);
        vm.WindowTitle = titleString[0];
        vm.Title = titleString[1];
        vm.TitleTooltip = titleString[1];
    }
}