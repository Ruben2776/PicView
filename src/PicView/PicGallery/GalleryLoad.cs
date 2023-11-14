using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.PicGallery;

internal static class GalleryLoad
{
    internal static bool IsLoading { get; set; }

    internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
    {
        // Add events and set fields, when it's loaded.
        UC.GetPicGallery.grid.MouseLeftButtonDown += (_, _) => ConfigureWindows.GetMainWindow.Focus();
        UC.GetPicGallery.x2.MouseLeftButtonDown += (_, _) => GalleryToggle.CloseHorizontalGallery();
    }

    internal class GalleryThumbHolder
    {
        internal string FileLocation { get; set; }
        internal string FileName { get; set; }
        internal string FileSize { get; set; }
        internal string FileDate { get; set; }
        internal BitmapSource BitmapSource { get; set; }

        internal GalleryThumbHolder(string fileLocation, string fileName, string fileSize, string fileDate,
            BitmapSource bitmapSource)
        {
            FileLocation = fileLocation;
            FileName = fileName;
            FileSize = fileSize;
            FileDate = fileDate;
            BitmapSource = bitmapSource;
        }

        internal static GalleryThumbHolder GetThumbData(int index)
        {
            var fileInfo = new FileInfo(Navigation.Pics[index]);
            var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[index], (int)GalleryNavigation.PicGalleryItemSize, fileInfo);
            var fileLocation = fileInfo.FullName;
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var fileSize = $"{(string)Application.Current.Resources["FileSize"]}: {fileInfo.Length.GetReadableFileSize()}";
            var fileDate = $"{(string)Application.Current.Resources["Modified"]}: {fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture)}";
            return new GalleryThumbHolder(fileLocation, fileName, fileSize, fileDate, bitmapSource);
        }
    }

    internal static async Task LoadAsync()
    {
        if (UC.GetPicGallery is null || IsLoading) { return; }

        IsLoading = true;
        var source = new CancellationTokenSource();
        var iterations = Navigation.Pics.Count;

        async Task UpdateThumbAsync(int i, int updates)
        {
            var galleryThumbHolderItem = await Task.FromResult(GalleryThumbHolder.GetThumbData(i)).ConfigureAwait(false);
            await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                UpdatePic(i, galleryThumbHolderItem.BitmapSource, galleryThumbHolderItem.FileLocation,
                    galleryThumbHolderItem.FileName, galleryThumbHolderItem.FileSize,
                    galleryThumbHolderItem.FileDate);
            }, DispatcherPriority.Background, source.Token);
            if (updates == iterations)
                IsLoading = false;
        }

        await Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    if (iterations != Navigation.Pics.Count)
                    {
                        await source.CancelAsync();
                        return;
                    }

                    var i1 = i;
                    await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                    {
                        Add(i1);
                    }, DispatcherPriority.DataBind, source.Token);
                }
                catch (Exception)
                {
                    GalleryFunctions.Clear();
                    return;
                }
            }

            var priority = iterations > 3000 ? DispatcherPriority.Background : DispatcherPriority.Render;
            var startPosition = 0;
            var updates = 0;
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                GalleryNavigation.SetSelected(Navigation.FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = Navigation.FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
                startPosition = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) % Navigation.Pics.Count;
                startPosition = startPosition < 0 ? 0 : startPosition;
            }, priority, source.Token);

            _ = Task.Run(async () =>
            {
                if (iterations > 3000)
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                }

                for (int i = startPosition; i < iterations; i++)
                {
                    updates++;
                    try
                    {
                        if (!IsLoading || Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || i > Navigation.Pics.Count)
                        {
                            throw new TaskCanceledException();
                        }
                        await UpdateThumbAsync(i, updates).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine(e.Message);
#endif
                        IsLoading = false;
                    }
                }
            }, source.Token).ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                if (iterations > 3000)
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                }
                for (var x = startPosition - 1; x >= 0; x--)
                {
                    updates++;
                    try
                    {
                        var i = x;

                        if (!IsLoading || Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || i > Navigation.Pics.Count)
                        {
                            throw new TaskCanceledException();
                        }
                        await UpdateThumbAsync(i, updates).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine(e.Message);
#endif
                        IsLoading = false;
                        return;
                    }
                }
            }, source.Token).ConfigureAwait(false);
        }, source.Token).ConfigureAwait(false);
    }

    internal static async Task ReloadGalleryAsync()
    {
        while (IsLoading)
        {
            await Task.Delay(200).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (UC.GetPicGallery.Container.Children.Count is 0)
                {
                    IsLoading = false;
                }
            });
        }
        await LoadAsync().ConfigureAwait(false);
    }

    internal static void Add(int i)
    {
        var selected = i == Navigation.FolderIndex;
        var item = new PicGalleryItem(null, i, selected);
        item.MouseLeftButtonUp += async delegate
        {
            await GalleryClick.ClickAsync(i).ConfigureAwait(false);
        };

        UC.GetPicGallery.Container.Children.Add(item);
    }

    internal static void UpdatePic(int i, BitmapSource? pic, string fileLocation, string fileName, string fileSize, string fileDate)
    {
        try
        {
            if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 || i >= UC.GetPicGallery.Container.Children.Count)
            {
                GalleryFunctions.Clear();
                return;
            }

            var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
            item.ThumbImage.Source = pic;
            item.MouseEnter += delegate
            {
                item.Popup.IsOpen = true;
            };
            item.MouseLeave += delegate
            {
                item.Popup.IsOpen = false;
            };
            item.ThumbFileLocation.Text = fileLocation;
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