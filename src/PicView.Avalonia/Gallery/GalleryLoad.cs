using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

public static class GalleryLoad
{
    public static bool IsLoading;
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
        IsLoading = true;

        foreach (var path in viewModel.ImageIterator.Pics)
        {
            var fileInfo = new FileInfo(path);
            
            var avaloniaImage = await ThumbnailHelper.GetThumb(fileInfo, (int)viewModel.GalleryItemSize);
            if (avaloniaImage is null)
            {
                continue;
            }
            
            if (currentDirectory != _currentDirectory)
            {
                viewModel.GalleryItems.Clear();
                return;
            }

            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(0, avaloniaImage as GalleryThumbInfo.IImageSource, fileInfo);
            thumbData.ThumbNailSize = viewModel.GalleryItemSize;
            viewModel.GalleryItems.Add(thumbData);
            if (path == viewModel.ImageIterator.Pics[viewModel.ImageIterator.Index])
            {
                viewModel.SelectedGalleryItem = thumbData;
            }
        }
        IsLoading = false;
    }
}