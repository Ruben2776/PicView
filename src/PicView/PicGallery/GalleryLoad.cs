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
        await Task.Run(async () =>
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
                        GalleryFunctions.Clear();
                        IsLoading = false;
                        source.Token.ThrowIfCancellationRequested();
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
                    return;
                }
            }

            if (iterations <= 700)
            {
                Parallel.For(0, iterations, (i, loopState) =>
                {
                    try
                    {
                        if (iterations != Navigation.Pics.Count || Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
                        {
                            throw new TaskCanceledException();
                        }

                        var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[i], (int)GalleryNavigation.PicGalleryItemSize);
                        var fileInfo = new FileInfo(Navigation.Pics[i]);
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
                        {
                            UpdatePic(i, bitmapSource, fileInfo);
                        }));
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine(e.Message);
#endif
                        IsLoading = false;
                        loopState.Stop();
                    }
                });
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    GalleryNavigation.SetSelected(Navigation.FolderIndex, true);
                    GalleryNavigation.SelectedGalleryItem = Navigation.FolderIndex;
                    GalleryNavigation.ScrollToGalleryCenter();
                    startPosition = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) % Navigation.Pics.Count;
                    startPosition = startPosition < 0 ? 0 : startPosition;
                }, DispatcherPriority.Render, source.Token);

                // Iterate forward from the starting position
                for (var x = startPosition; x < iterations; x++)
                {
                    await UpdatePicAsync(x, iterations).ConfigureAwait(false);
                }

                // Iterate up to the beginning of the previous iteration
                for (var x = startPosition - 1; x >= 0; x--)
                {
                    await UpdatePicAsync(x, iterations).ConfigureAwait(false);
                }
            }
        }, source.Token).ConfigureAwait(false);
        IsLoading = false;
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

    internal static void Add(int i, int index)
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
        {
            var selected = i == index;
            var item = new PicGalleryItem(null, i, selected);
            item.MouseLeftButtonUp += async delegate
            {
                await GalleryClick.ClickAsync(i).ConfigureAwait(false);
            };

            UC.GetPicGallery.Container.Children.Add(item);
        });
    }

    internal static void UpdatePic(int i, BitmapSource? pic, FileInfo fileInfo)
    {
        try
        {
            if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 || i >= UC.GetPicGallery.Container.Children.Count)
            {
                GalleryFunctions.Clear();
                return;
            }

            var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
            item.ThumbImage.Source = pic ?? ImageFunctions.ShowLogo();
            item.MouseEnter += delegate
            {
                item.Popup.IsOpen = true;
            };
            item.MouseLeave += delegate
            {
                item.Popup.IsOpen = false;
            };
            item.ThumbFileLocation.Text = fileInfo.FullName;
            item.ThumbFileName.Text = Path.GetFileNameWithoutExtension(fileInfo.Name);
            item.ThumbFileSize.Text = $"{(string)Application.Current.Resources["FileSize"]}: {fileInfo.Length.GetReadableFileSize()}";
            item.ThumbFileDate.Text = $"{(string)Application.Current.Resources["Modified"]}: {fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture)}";
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e.Message);
#endif
            // Suppress task cancellation
        }
    }

    internal static async Task UpdatePicAsync(int index, int iterations)
    {
        if (iterations != Navigation.Pics.Count || Navigation.Pics.Count < 1 || index > Navigation.Pics.Count)
        {
            return;
        }
        var bitmapSource = await Task.FromResult(Thumbnails.GetBitmapSourceThumb(Navigation.Pics[index], (int)GalleryNavigation.PicGalleryItemSize)).ConfigureAwait(false);
        if (Navigation.Pics.Count < 1 || index > Navigation.Pics.Count)
        {
            return;
        }
        var fileInfo = new FileInfo(Navigation.Pics[index]);
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            UpdatePic(index, bitmapSource, fileInfo);
        }, DispatcherPriority.Render);
    }
}