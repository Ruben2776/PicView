using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.PicGallery;

internal static class GalleryLoad
{
    internal static bool IsLoading { get; private set; }

    internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
    {
        // Add events and set fields, when it's loaded.
        UC.GetPicGallery.grid.MouseLeftButtonDown += (_, _) => ConfigureWindows.GetMainWindow.Focus();
        UC.GetPicGallery.x2.MouseLeftButtonDown += (_, _) => GalleryToggle.CloseHorizontalGallery();
    }

    internal static async Task LoadAsync()
    {
        if (UC.GetPicGallery is null || IsLoading) { return; }

        IsLoading = true;
        var source = new CancellationTokenSource();
        await Task.Run(() =>
        {
            var iterations = Navigation.Pics.Count;
            var index = Navigation.FolderIndex;
            var startPosition = 0;

            for (var i = 0; i < iterations; i++)
            {
                try
                {
                    if (iterations != Navigation.Pics.Count)
                    {
                        throw new TaskCanceledException();
                    }

                    Add(i, index);
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine(e.Message);
#endif
                    GalleryFunctions.Clear();
                    IsLoading = false;
                }
            }

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                GalleryNavigation.SetSelected(Navigation.FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = Navigation.FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
                startPosition = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) % Navigation.Pics.Count;
                startPosition = startPosition < 0 ? 0 : startPosition;
            }, DispatcherPriority.Render, source.Token);

            // Iterate forward from the starting position
            for (int x = startPosition; x < iterations; x++)
            {
                var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[x], (int)GalleryNavigation.PicGalleryItemSize);
                if (iterations != Navigation.Pics.Count || Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
                {
                    IsLoading = false;
                    return;
                }

                UpdatePic(x, bitmapSource);
            }

            // Iterate up to the beginning of the previous iteration
            for (int x = 0; x < startPosition; x++)
            {
                var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[x], (int)GalleryNavigation.PicGalleryItemSize);
                if (iterations != Navigation.Pics.Count || Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
                {
                    IsLoading = false;
                    return;
                }

                UpdatePic(x, bitmapSource);
            }
        }, source.Token).ConfigureAwait(false);
        IsLoading = false;
    }

    internal static async Task ReloadGallery()
    {
        if (Settings.Default.IsBottomGalleryShown)
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
    }

    internal static void Add(int i, int index)
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
        {
            var selected = i == index;
            var item = new PicGalleryItem(null, i, selected);
            item.MouseLeftButtonDown += async delegate
            {
                await GalleryClick.ClickAsync(i).ConfigureAwait(false);
            };

            UC.GetPicGallery.Container.Children.Add(item);
        });
    }

    internal static void UpdatePic(int i, BitmapSource? pic)
    {
        try
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                item.ThumbImage.Source = pic ?? ImageFunctions.ShowLogo();
            }));
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