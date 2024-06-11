using PicView.Core.Gallery;
using PicView.WPF.ChangeImage;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.WPF.PicGallery;

internal static class GalleryLoad
{
    /// <summary>
    /// Gets or sets a value indicating whether the gallery is currently loading.
    /// </summary>
    internal static bool IsLoading { get; set; }

    /// <summary>
    /// Event handler for the PicGallery Loaded event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
    {
        // Add events and set fields when it's loaded.
        UC.GetPicGallery.grid.MouseLeftButtonDown += (_, _) => ConfigureWindows.GetMainWindow.Focus();
        UC.GetPicGallery.x2.MouseLeftButtonDown += (_, _) => GalleryToggle.CloseHorizontalGallery();
    }

    /// <summary>
    /// Asynchronously loads the gallery.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task LoadAsync()
    {
        if (UC.GetPicGallery is null || IsLoading || Navigation.Pics is null)
        {
            return;
        }

        if (Navigation.Pics.Count <= 0)
        {
            return;
        }

        IsLoading = true;
        using var source = new CancellationTokenSource();
        var updates = 0;
        var count = Navigation.Pics.Count;

        // Update UI in batch sizes and await task delay to ensure responsive UI
        var batchSize = 200;
        for (var start = 0; start < Navigation.Pics.Count; start += batchSize)
        {
            if (Navigation.Pics.Count <= 0)
                return;

            var end = Math.Min(start + batchSize, Navigation.Pics.Count);
            for (var i = start; i < end; i++)
            {
                if (Navigation.Pics.Count <= 0)
                    return;

                try
                {
                    var i1 = i;
                    await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                    {
                        if (!IsLoading)
                        {
                            source.Cancel();
                            source.Dispose();
                            return;
                        }

                        Add(i1);
                    }, DispatcherPriority.DataBind, source.Token);
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)} exception:\n{exception.Message}");
#endif
                    GalleryFunctions.Clear();
                    return;
                }
            }
            await Task.Delay(50, source.Token);
        }

        if (source.IsCancellationRequested)
        {
            return;
        }

        var index = 0;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (GalleryNavigation.HorizontalItems is 0 || Navigation.Pics.Count is 0)
            {
                source.Cancel();
                source.Dispose();
                return;
            }

            GalleryNavigation.SetSelected(Navigation.FolderIndex, true);
            GalleryNavigation.SelectedGalleryItem = Navigation.FolderIndex;
            GalleryNavigation.ScrollToGalleryCenter();

            index = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) %
                    Navigation.Pics.Count;
            index = index < 0 ? 0 : index;
        }, DispatcherPriority.DataBind, source.Token);

        try
        {
            ParallelOptions options = new()
            {
                // Don't slow the system down too much
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 2, 4)
            };
            ParallelOptions secondOptions = new()
            {
                MaxDegreeOfParallelism = 2
            };
            var positiveLoopTask = Loop(positive: true, index, Navigation.Pics.Count, options, source);
            var negativeLoopTask = Loop(positive: false, 0, index, secondOptions, source);

            await Task.WhenAll(positiveLoopTask, negativeLoopTask).ConfigureAwait(false);
            IsLoading = false;
            
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(GalleryStretch.SetStretchMode, DispatcherPriority.DataBind, source.Token);
           
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadAsync)} exception:\n{exception.Message}");
#endif
            if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
            {
                // Fix window not shutting down when it's supposed to. MainWindow is only hidden when closing
                Environment.Exit(0);
            }
        }
        return;

        async Task Loop(bool positive, int startPosition, int end, ParallelOptions parallelOptions, CancellationTokenSource tokenSource)
        {
            await Parallel.ForAsync(0, end, parallelOptions, async (i, _) =>
            {
                try
                {
                    if (Navigation.Pics.Count == 0 || count != Navigation.Pics.Count)
                    {
                        await tokenSource.CancelAsync();
                        return;
                    }
                    var nextIndex = positive ?
                        (startPosition + i) % end :
                        (startPosition - i + end) % end;

                    if (!tokenSource.IsCancellationRequested)
                    {
                        await UpdateThumbAsync(nextIndex, tokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                    if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
                    {
                        // Fix window not shutting down when it's supposed to. MainWindow is only hidden when closing
                        Environment.Exit(0);
                    }
                }
            });
        }

        async Task UpdateThumbAsync(int i, CancellationToken token)
        {
            var fileInfo = new FileInfo(Navigation.Pics[i]);
            var bitmapSource = await Thumbnails.GetBitmapSourceThumbAsync(Navigation.Pics[i],
                (int)GalleryNavigation.PicGalleryItemSize, fileInfo).ConfigureAwait(false);
            var converted = new Image2BitmapSource.WpfImageSource(bitmapSource);
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(i, converted, fileInfo);
            await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                UpdatePic(i, bitmapSource, thumbData.FileLocation,
                    thumbData.FileName, thumbData.FileSize,
                    thumbData.FileDate);
            }, DispatcherPriority.Background, token);
            updates++;
            if (updates >= Navigation.Pics.Count)
                IsLoading = false;
        }
    }

    internal static async Task ReloadGalleryAsync()
    {
        while (IsLoading)
        {
            await Task.Delay(200).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (UC.GetPicGallery.Container.Children.Count is 0)
                    {
                        IsLoading = false;
                    }
                }
                catch (Exception)
                {
                    //
                }
            });
        }

        await LoadAsync().ConfigureAwait(false);
    }

    internal static void Add(int i)
    {
        if (Navigation.Pics is { Count: 0 })
        {
            return;
        }

        if (i < 0 || i >= Navigation.Pics.Count)
        {
            return;
        }

        var item = new PicGalleryItem(null, Navigation.Pics[i], false);
        item.MouseLeftButtonUp += async delegate { await GalleryClick.ClickAsync(i).ConfigureAwait(false); };

        UC.GetPicGallery.Container.Children.Add(item);
    }

    internal static void UpdatePic(int i, ImageSource? pic, string fileLocation, string fileName, string fileSize,
        string fileDate)
    {
        try
        {
            if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 ||
                i >= UC.GetPicGallery.Container.Children.Count)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(UpdatePic)} wrong index {Navigation.Pics?.Count}," +
                                $" {Navigation.FolderIndex}, {UC.GetPicGallery?.Container?.Children.Count}");
#endif
                return;
            }
            var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
            item.ThumbImage.Source = pic;
            item.MouseEnter += delegate { item.Popup.IsOpen = true; };
            item.MouseLeave += delegate { item.Popup.IsOpen = false; };
            item.UpdateValues(fileName, fileDate, fileSize, fileLocation);

            item.AddContextMenu();
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e.Message);
#endif
            // Suppress task cancellation
        }
    }
}