using PicView.Core.Gallery;
using PicView.WPF.ChangeImage;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
        if (UC.GetPicGallery is null || IsLoading)
        {
            return;
        }

        IsLoading = true;
        using var source = new CancellationTokenSource();
        var updates = 0;

        for (var i = 0; i < Navigation.Pics.Count; i++)
        {
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
                Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                GalleryFunctions.Clear();
                return;
            }
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
                CancellationToken = source.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount - 2 < 1 ? 1 : Environment.ProcessorCount - 2
            };
            await Loop(index, Navigation.Pics.Count, options).ConfigureAwait(false);
            await Loop(0, index, options).ConfigureAwait(false);
            IsLoading = false;
        }
        catch (ObjectDisposedException exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
            if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
            {
                Environment.Exit(0);
            }
        }
        catch (TaskCanceledException exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
            if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
            {
                Environment.Exit(0);
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
        }
        return;

        async Task Loop(int startPosition, int end, ParallelOptions options)
        {
            await Parallel.ForAsync(startPosition, end, options, async (i, loopState) =>
            {
                updates++;
                try
                {
                    if (!IsLoading || Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || i > Navigation.Pics.Count)
                    {
                        IsLoading = false;
                        await source.CancelAsync().ConfigureAwait(false);
                        loopState.ThrowIfCancellationRequested();
                        return;
                    }

                    if (!source.IsCancellationRequested)
                    {
                        await UpdateThumbAsync(i, source.Token).ConfigureAwait(false);
                    }
                }
                catch (ObjectDisposedException exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                    if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
                    {
                        Environment.Exit(0);
                    }
                }
                catch (TaskCanceledException exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                    if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
                    {
                        Environment.Exit(0);
                    }
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(LoadAsync)}  exception:\n{exception.Message}");
#endif
                }
            });
        }

        async Task UpdateThumbAsync(int i, CancellationToken token)
        {
            var fileInfo = new FileInfo(Navigation.Pics[i]);
            var bitmapSource = await Thumbnails.GetBitmapSourceThumbAsync(Navigation.Pics[i],
                (int)GalleryNavigation.PicGalleryItemSize, fileInfo).ConfigureAwait(false);
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(i, bitmapSource, fileInfo);
            await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                UpdatePic(i, bitmapSource, thumbData.FileLocation,
                    thumbData.FileName, thumbData.FileSize,
                    thumbData.FileDate);
            }, DispatcherPriority.Background, token);
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

        var selected = i == Navigation.FolderIndex;
        var item = new PicGalleryItem(null, Navigation.Pics[i], selected);
        item.MouseLeftButtonUp += async delegate { await GalleryClick.ClickAsync(i).ConfigureAwait(false); };

        UC.GetPicGallery.Container.Children.Add(item);
    }

    internal static void UpdatePic(int i, BitmapSource? pic, string fileLocation, string fileName, string fileSize,
        string fileDate)
    {
        try
        {
            if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 ||
                i >= UC.GetPicGallery.Container.Children.Count)
            {
                GalleryFunctions.Clear();
                return;
            }

            var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
            item.ThumbImage.Source = pic;
            item.MouseEnter += delegate { item.Popup.IsOpen = true; };
            item.MouseLeave += delegate { item.Popup.IsOpen = false; };
            item.ThumbFileLocation.Text = item.FileName = fileLocation;
            item.ThumbFileName.Text = fileName;
            item.ThumbFileSize.Text = fileSize;
            item.ThumbFileDate.Text = fileDate;

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