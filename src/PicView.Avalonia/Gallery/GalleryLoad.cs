using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

public static class GalleryLoad
{
    private static bool _isLoading;
    private static string? _currentDirectory;

    public static async Task LoadGallery(MainViewModel viewModel, string currentDirectory)
    {
        if (viewModel.ImageIterator.Pics.Count == 0)
        {
            return;
        }

        if (viewModel.GalleryItems is null)
        {
            viewModel.GalleryItems = [];
        }
        else if (viewModel.GalleryItems.Count > 0)
        {
            if (string.IsNullOrEmpty(_currentDirectory))
            {
                return;
            }
            if (currentDirectory == _currentDirectory)
            {
                return;
            }
            viewModel.GalleryItems.Clear();
        }

        _currentDirectory = currentDirectory;
        _isLoading = true;
        var count = viewModel.ImageIterator.Pics.Count;

        foreach (var item in viewModel.ImageIterator.Pics)
        {
            object? avaloniaImage = null;
            FileInfo? fileInfo = null;
            try
            {
                var magick = new MagickImage();
                fileInfo = new FileInfo(item);
                if (fileInfo.Length >= 2147483648)
                {
                    await using var fileStream = new FileStream(item, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite, 4096, true);
                    // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                    // ReSharper disable once MethodHasAsyncOverload
                    magick?.Read(fileStream);
                }
                else
                {
                    await magick.ReadAsync(item);
                }

                var geometry = new MagickGeometry(100, 100);
                magick?.Thumbnail(geometry);
                magick.Format = MagickFormat.Png;
                await using var memoryStream = new MemoryStream();
                await magick.WriteAsync(memoryStream);
                memoryStream.Position = 0;
                var bmp = new Bitmap(memoryStream);
                avaloniaImage = new ImageService.AvaloniaImageSource(bmp);
                if (currentDirectory != _currentDirectory || count != viewModel.ImageIterator.Pics.Count)
                {
                    viewModel.GalleryItems.Clear();
                    return;
                }
            }
            catch (Exception)
            {
                //
            }
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(0, avaloniaImage as GalleryThumbInfo.IImageSource, fileInfo);
            viewModel.GalleryItems.Add(thumbData);
        }

        //ParallelOptions options = new()
        //{
        //    // Don't slow the system down too much
        //    MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 2, 4)
        //};
        //await Parallel.ForAsync(0, count, options, async (i, cancellationToken) =>
        //{
        //    try
        //    {
        //        var item = viewModel.ImageIterator.Pics[i];
        //        object? avaloniaImage = null;
        //        FileInfo? fileInfo = null;

        //        var magick = new MagickImage();
        //        fileInfo = new FileInfo(item);
        //        if (fileInfo.Length >= 2147483648)
        //        {
        //            await using var fileStream = new FileStream(item, FileMode.Open, FileAccess.Read,
        //                FileShare.ReadWrite, 4096, true);
        //             Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
        //             ReSharper disable once MethodHasAsyncOverload
        //            magick?.Read(fileStream);
        //        }
        //        else
        //        {
        //            await magick.ReadAsync(item, cancellationToken);
        //        }

        //        var geometry = new MagickGeometry(100, 100);
        //        magick?.Thumbnail(geometry);
        //        magick.Format = MagickFormat.Png;
        //        await using var memoryStream = new MemoryStream();
        //        await magick.WriteAsync(memoryStream, cancellationToken);
        //        memoryStream.Position = 0;
        //        var bmp = new Bitmap(memoryStream);
        //        avaloniaImage = new ImageService.AvaloniaImageSource(bmp);

        //        if (currentDirectory != _currentDirectory || count != viewModel.ImageIterator.Pics.Count)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();
        //            return;
        //        }

        //        var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(0,
        //            avaloniaImage as GalleryThumbInfo.IImageSource, fileInfo);
        //        viewModel.GalleryItems.Add(thumbData);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //});

        _isLoading = false;
    }
}