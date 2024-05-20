#if DEBUG
using System.Diagnostics;
#endif
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
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
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        MainView? mainView;
        ListBox? galleryListBox = null;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            galleryListBox = mainView.GalleryView.GalleryListBox;
        });

        if (IsLoading)
        {
            if (string.IsNullOrEmpty(_currentDirectory) || currentDirectory == _currentDirectory)
            {
                return;
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox?.Items.Clear();
            });
        }
        // ReSharper disable once MethodHasAsyncOverload
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _currentDirectory = currentDirectory;
        IsLoading = true;
        var cancellationToken = _cancellationTokenSource.Token;
        var index = viewModel.ImageIterator.Index;

        try
        {
            await Loop(0, viewModel.ImageIterator.Pics.Count, cancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox.ScrollIntoView(viewModel.SelectedGalleryItemIndex);
            });

            var maxDegreeOfParallelism = Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : 2;
            ParallelOptions options = new() { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            await AsyncLoop(positive:true, index, viewModel.ImageIterator.Pics.Count, options, cancellationToken);
            await AsyncLoop(positive:false, 0, index, options, cancellationToken);
            
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox?.Items.Clear();
            });
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
                if (currentDirectory != _currentDirectory || ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        galleryListBox?.Items.Clear();
                    });
                    return;
                }

                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var galleryItem = new GalleryItem
                        {
                            DataContext = viewModel,
                        };
                        galleryListBox.Items.Add(galleryItem);
                    }, DispatcherPriority.Background, ct);
                    if (i == viewModel.ImageIterator.Index)
                    {
                        viewModel.SelectedGalleryItemIndex = i;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            galleryListBox.ScrollIntoView(viewModel.SelectedGalleryItemIndex);
                        });
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine(e.ToString());
#endif
                }
            }
        }

        async Task AsyncLoop(bool positive, int startIndex, int endIndex, ParallelOptions options, CancellationToken ct)
        {
            await Parallel.ForAsync(startIndex, endIndex, options, async (i, _) =>
            {
                ct.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(viewModel.ImageIterator.Pics[i]);
                var thumbImageModel = await ImageHelper.GetImageModelAsync(fileInfo, isThumb: true,
                    (int)viewModel.GalleryItemSize);
                var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(i, null, fileInfo);


                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
                    var galleryListBox = mainView.GalleryView.GalleryListBox;
                    if (galleryListBox.Items[i] is not GalleryItem galleryItem)
                    {
                        return;
                    }

                    if (thumbImageModel?.Image is not null)
                    {
                        ImageHelper.SetImage(thumbImageModel.Image, galleryItem.GalleryImage,
                            thumbImageModel.ImageType);
                    }

                    galleryItem.FileLocation.Text = thumbData.FileLocation ?? "";
                    galleryItem.FileDate.Text = thumbData.FileDate ?? "";
                    galleryItem.FileSize.Text = thumbData.FileSize ?? "";
                    galleryItem.FileName.Text = thumbData.FileName ?? "";
                    galleryItem.PointerPressed += async (_, _) =>
                    {
                        if (GalleryFunctions.IsFullGalleryOpen)
                        {
                            await GalleryFunctions.ToggleGallery(viewModel);
                        }
                        await viewModel.ImageIterator.LoadPicAtIndex(i, viewModel);
                    };
                }, DispatcherPriority.Background, ct);
            });
        }
    }
}
