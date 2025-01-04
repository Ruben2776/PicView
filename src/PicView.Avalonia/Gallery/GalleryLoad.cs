using Avalonia.Svg.Skia;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

public static class GalleryLoad
{
    public static bool IsLoading { get; private set; }
    private static string? _currentDirectory;
    private static CancellationTokenSource? _cancellationTokenSource;

    public static async Task LoadGallery(MainViewModel vm, string currentDirectory)
    {
        // TODO: Lazy load this when scrolling instead. Figure out how to support virtualization. 
        
        // Make sure height is set
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && !GalleryFunctions.IsFullGalleryOpen)
        {
            vm.GetGalleryItemHeight = vm.GetBottomGalleryItemHeight;
        }
        
        if (vm.ImageIterator?.ImagePaths.Count == 0 || IsLoading || vm.ImageIterator is null)
        {
            return;
        }
        
        var mainView = UIHelper.GetMainView;
        var galleryListBox = mainView.GalleryView.GalleryListBox;
        var toReturn = false;

        if (!string.IsNullOrEmpty(_currentDirectory))
        {
            if (_currentDirectory == currentDirectory)
            {
                return;
            }
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (galleryListBox is null)
            {
                toReturn = true;
                return;
            }
            if (galleryListBox.Items.Count > 0)
            {
                // Make sure to not run consecutively
                toReturn = true;
            }
        });
        if (toReturn)
        {
            return;
        }
    
        _cancellationTokenSource = new CancellationTokenSource();
        _currentDirectory = currentDirectory;
        IsLoading = true;
        var index = vm.ImageIterator.CurrentIndex;
        var galleryItemSize = Math.Max(vm.GetBottomGalleryItemHeight, vm.GetFullGalleryItemHeight);
        
        var endIndex = vm.ImageIterator.ImagePaths.Count;
        // Set priority low when loading excess images to ensure app responsiveness
        var priority = endIndex switch
        {
            >= 2000 => DispatcherPriority.Background,
            >= 1000 => DispatcherPriority.Loaded,
            _ => DispatcherPriority.Render
        };

        GalleryStretchMode.SetSquareFillStretch(vm);
        var fileInfos = new FileInfo[endIndex];
        try
        {
            for (var i = 0; i < endIndex; i++)
            {
                if (currentDirectory != _currentDirectory || _cancellationTokenSource.IsCancellationRequested || vm.ImageIterator is null)
                {
                    await _cancellationTokenSource.CancelAsync();
                    return;
                }
                
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                fileInfos[i] = new FileInfo(vm.ImageIterator.ImagePaths[i]);
                var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(fileInfos[i]);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var galleryItem = new GalleryItem
                    {
                        DataContext = vm,
                        FileName = { Text = thumbData.FileName },
                        FileSize = { Text = thumbData.FileSize },
                        FileDate = { Text = thumbData.FileDate },
                        FileLocation = { Text = fileInfos[i].FullName },
                    };
                    var i1 = i;
                    galleryItem.PointerPressed += async (_, _) =>
                    {
                        if (GalleryFunctions.IsFullGalleryOpen)
                        {
                            await GalleryFunctions.ToggleGallery(vm);
                        }
                        await NavigationHelper.Navigate(vm.ImageIterator.ImagePaths.IndexOf(fileInfos[i1].FullName), vm).ConfigureAwait(false);
                    };
                    galleryListBox.Items.Add(galleryItem);
                    if (i != vm.ImageIterator?.CurrentIndex)
                    {
                        return;
                    }

                    vm.SelectedGalleryItemIndex = i;
                    galleryListBox.SelectedItem = galleryItem;
                }, priority, _cancellationTokenSource.Token);
            }
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (galleryListBox.Items.Count == 0)
                {
                    return;
                }
                if (galleryListBox.Items[0] is not GalleryItem galleryItem)
                {
                    return;
                }
                var horizontalItems = (int)Math.Floor(galleryListBox.Bounds.Width / galleryItem.ImageBorder.MinWidth);
                index = (vm.ImageIterator.CurrentIndex - horizontalItems) % vm.ImageIterator.ImagePaths.Count;
            });

            index = index < 0 ? 0 : index;
            var maxDegreeOfParallelism = Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : 2;
            ParallelOptions options = new() { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            await AsyncLoop(index, vm.ImageIterator.ImagePaths.Count, options, _cancellationTokenSource.Token);
            await AsyncLoop(0, index, options, _cancellationTokenSource.Token);
            GalleryStretchMode.DetermineStretchMode(vm);
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(GalleryFunctions.Clear);
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine($"GalleryLoad exception:\n{e.Message}");
#endif
        }
        finally
        {
            IsLoading = false;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _currentDirectory = null;
        }

        return;

        async Task AsyncLoop(int startIndex, int endIndex, ParallelOptions options, CancellationToken ct)
        {
            await Parallel.ForAsync(startIndex, endIndex, options, async (i, _) =>
            {
                if (currentDirectory != _currentDirectory || _cancellationTokenSource.IsCancellationRequested || vm.ImageIterator is null)
                {
                    await _cancellationTokenSource.CancelAsync();
                    return;
                }
                ct.ThrowIfCancellationRequested();

                if (i < 0 || i >= vm.ImageIterator.ImagePaths.Count)
                {
                    return;
                }

                var thumb = await GetThumbnails.GetThumbAsync(fileInfos[i].FullName, (uint)galleryItemSize, fileInfos[i]);

                var isSvg = fileInfos[i].Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
                           fileInfos[i].Extension.Equals(".svgz", StringComparison.OrdinalIgnoreCase);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (i < 0 || i >= galleryListBox.Items.Count)
                    {
                        return;
                    }
                    if (galleryListBox.Items[i] is not GalleryItem galleryItem)
                    {
                        return;
                    }

                    if (isSvg)
                    {
                        galleryItem.GalleryImage.Source = new SvgImage
                            { Source = SvgSource.Load(fileInfos[i].FullName) };
                    }
                    else if (thumb is not null)
                    {
                        galleryItem.GalleryImage.Source = thumb;
                    }
                    
                    if (vm.ImageIterator is null)
                    {
                        ct.ThrowIfCancellationRequested();
                        GalleryFunctions.Clear();
                        if (GalleryFunctions.IsBottomGalleryOpen)
                        {
                            mainView.GalleryView.GalleryMode = GalleryMode.BottomToClosed;
                        }
                        
                        return;
                    }

                    if (i == vm.ImageIterator.CurrentIndex)
                    {
                        galleryListBox.ScrollToCenterOfItem(galleryItem);
                    }
                }, priority, ct);
            });
        }
    }
    
    public static async Task ReloadGalleryAsync(MainViewModel vm, string currentDirectory)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    GalleryFunctions.Clear();
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine($"GalleryLoad exception:\n{e.Message}");
#endif
                }
            });
        }

        var x = 0;
        while (IsLoading)
        {
            await Task.Delay(200).ConfigureAwait(false);
            x++;
            if (x > 100)
            {
                break;
            }
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                GalleryFunctions.Clear();
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"GalleryLoad exception:\n{e.Message}");
#endif
            }
        });

        await LoadGallery(vm, currentDirectory).ConfigureAwait(false);
    }
}
