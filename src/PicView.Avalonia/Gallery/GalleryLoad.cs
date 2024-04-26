using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Helpers;
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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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

        for (var i = 0; i < viewModel.ImageIterator.Pics.Count; i++)
        {
            var item = viewModel.ImageIterator.Pics[i];
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

                var geometry = new MagickGeometry(0, (int)viewModel.GalleryItemSize);
                magick?.Thumbnail(geometry);
                magick.Format = MagickFormat.Png;
                await using var memoryStream = new MemoryStream();
                await magick.WriteAsync(memoryStream);
                memoryStream.Position = 0;
                var bmp = new Bitmap(memoryStream);
                avaloniaImage = new AvaloniaImageSource(bmp);
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
            thumbData.ThumbNailSize = viewModel.GalleryItemSize;
            viewModel.GalleryItems.Add(thumbData);
            if (i == viewModel.ImageIterator.Index)
            {
                viewModel.SelectedGalleryItem = thumbData;
            }
        }
        _isLoading = false;
    }
}