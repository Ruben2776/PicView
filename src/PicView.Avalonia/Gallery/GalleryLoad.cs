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
            var path = viewModel.ImageIterator.Pics[i];
            var fileInfo = new FileInfo(path);
            
            var avaloniaImage = await ThumbnailHelper.GetThumb(fileInfo, (int)viewModel.GalleryItemSize);
            if (avaloniaImage is null)
            {
                continue;
            }
            
            if (currentDirectory != _currentDirectory || count != viewModel.ImageIterator.Pics.Count)
            {
                viewModel.GalleryItems.Clear();
                return;
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