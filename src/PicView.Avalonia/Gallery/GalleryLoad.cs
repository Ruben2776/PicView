#if DEBUG
using System.Diagnostics;
#endif
using System.Collections.ObjectModel;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

public static class GalleryLoad
{
    public static bool IsLoading { get; private set; }
    private static string? _currentDirectory;
    private static CancellationTokenSource? _cancellationTokenSource;

    public static async Task LoadGallery(MainViewModel viewModel, string currentDirectory)
    {
        if (viewModel.ImageIterator.Pics.Count == 0)
        {
            return;
        }

        if (viewModel.GalleryItems == null)
        {
            viewModel.GalleryItems = new ObservableCollection<GalleryViewModel>();
        }
        else if (viewModel.GalleryItems.Count > 0)
        {
            if (string.IsNullOrEmpty(_currentDirectory) || currentDirectory == _currentDirectory)
            {
                return;
            }
            viewModel.GalleryItems.Clear();
        }
        
        // ReSharper disable once MethodHasAsyncOverload
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _currentDirectory = currentDirectory;
        IsLoading = true;

        var fileInfos = new FileInfo[viewModel.ImageIterator.Pics.Count];
        var cancellationToken = _cancellationTokenSource.Token;
        
        try
        {
            await Loop(0, viewModel.ImageIterator.Pics.Count, cancellationToken);
            
            var maxDegreeOfParallelism = Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : 2;
            ParallelOptions options = new() { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            const int batchSize = 25;
            
            for (var start = 0; start < viewModel.ImageIterator.Pics.Count; start += batchSize)
            {
                var end = Math.Min(start + batchSize, viewModel.ImageIterator.Pics.Count);
                await AsyncLoop(start, end, options, cancellationToken);
                await Task.Delay(5, cancellationToken);
            }
            
        }
        catch (OperationCanceledException)
        {
            viewModel.GalleryItems.Clear();
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"GalleryLoad exception:\n{e.Message}");
#endif
        }
        finally
        {
            IsLoading = false;
        }

        return;

        async Task Loop(int startIndex, int endIndex, CancellationToken ct)
        {
            for (var i = startIndex; i < endIndex; i++)
            {
                await Task.Delay(3, cancellationToken).ConfigureAwait(false);
                if (currentDirectory != _currentDirectory || ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    viewModel.GalleryItems.Clear();
                    return;
                }

                var galleryViewModel = new GalleryViewModel(viewModel.GalleryItemSize);
                fileInfos[i] = new FileInfo(viewModel.ImageIterator.Pics[i]);
                var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(0, null, fileInfos[i]);
                galleryViewModel.FileLocation = thumbData.FileLocation;
                galleryViewModel.FileDate = thumbData.FileDate;
                galleryViewModel.FileSize = thumbData.FileSize;
                galleryViewModel.FileName = thumbData.FileName;
                viewModel.GalleryItems.Add(galleryViewModel);
                
                if (i == viewModel.ImageIterator.Index)
                {
                    viewModel.SelectedGalleryItemIndex = i;
                }
            }
        }
        async Task AsyncLoop(int startIndex, int endIndex, ParallelOptions options, CancellationToken ct)
        {
            await Parallel.ForAsync(startIndex, endIndex, options, async (i, _) =>
            {
                ct.ThrowIfCancellationRequested();
                var fileInfo = fileInfos[i];
                var galleryViewModel = viewModel.GalleryItems[i];
                galleryViewModel.ImageSource = await ThumbnailHelper.GetThumb(fileInfo, (int)galleryViewModel.GalleryItemSize);
                
            });
        }
    }
}
