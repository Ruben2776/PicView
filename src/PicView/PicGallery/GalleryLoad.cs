using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.PicGallery;

internal static class GalleryLoad
{
    internal static bool IsLoading { get; private set; }

    internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
    {
        // Add events and set fields, when it's loaded.
        UC.GetPicGallery.Scroller.ScrollChanged += (_, _) => ConfigureWindows.GetMainWindow.Focus(); // Maintain window focus when scrolling manually
        UC.GetPicGallery.grid.MouseLeftButtonDown += (_, _) => ConfigureWindows.GetMainWindow.Focus();
        UC.GetPicGallery.x2.MouseLeftButtonDown += (_, _) => GalleryToggle.CloseHorizontalGallery();
        UC.GetPicGallery.Scroller.ScrollChanged += async (_, e) =>
        {
            if (Navigation.Pics.Count < 4000)
            {
                return;
            }

            if (UC.GetPicGallery.Scroller.HorizontalOffset is 0)
            {
                return;
            }

            double horizontalOffset = UC.GetPicGallery.Scroller.HorizontalOffset;
            double viewportWidth = UC.GetPicGallery.Scroller.ViewportWidth;
            double itemWidth = GalleryNavigation.PicGalleryItemSize;
            int firstVisibleIndex = (int)Math.Floor(horizontalOffset / itemWidth / GalleryNavigation.HorizontalItems);
            //int firstVisibleIndex = true ? Math.Max(Navigation.FolderIndex - GalleryNavigation.HorizontalItems, 0) : (int)Math.Floor(horizontalOffset / itemWidth);
            int lastVisibleIndex = Math.Max(firstVisibleIndex + GalleryNavigation.HorizontalItems * 3, Navigation.Pics.Count);
            //int lastVisibleIndex = (int)Math.Ceiling((horizontalOffset + viewportWidth) / itemWidth) - 1;

            // Adjust the range based on the number of items in Navigation.Pics
            int itemCount = Navigation.Pics.Count;
            firstVisibleIndex = Math.Max(0, Math.Min(firstVisibleIndex, itemCount - 1));
            lastVisibleIndex = Math.Max(0, Math.Min(lastVisibleIndex, itemCount - 1));

            var source = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = Navigation.Pics.Count;

                Parallel.For(firstVisibleIndex, lastVisibleIndex, (i, loopState) =>
                {
                    try
                    {
                        if (count != Navigation.Pics.Count || Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
                        {
                            throw new TaskCanceledException();
                        }

                        var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[i], (int)GalleryNavigation.PicGalleryItemSize);
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() =>
                        {
                            if (i >= UC.GetPicGallery.Container.Children.Count)
                            {
                                return;
                            }
                            var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                            item.ThumbImage.Source = bitmapSource;
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
            }, source.Token).ConfigureAwait(false);

            source.Dispose();
        };
    }

    internal static void LoadLayout()
    {
        if (UC.GetPicGallery == null)
        {
            UC.GetPicGallery = new Views.UserControls.Gallery.PicGallery
            {
                Opacity = 0
            };
            Panel.SetZIndex(UC.GetPicGallery, 2);
            ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(UC.GetPicGallery);
        }

        if (Settings.Default.IsBottomGalleryShown && GalleryFunctions.IsGalleryOpen == false)
        {
            LoadBottomGallery();
            ScaleImage.TryFitImage();
        }
        else
        {
            // Make sure booleans are correct
            GalleryFunctions.IsGalleryOpen = true;

            // Set size
            GalleryNavigation.SetSize(Settings.Default.ExpandedGalleryItemSize);
            UC.GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
            UC.GetPicGallery.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;

            // Set alignment
            UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
            UC.GetPicGallery.VerticalAlignment = VerticalAlignment.Stretch;

            // Set scrollbar visibility and orientation
            UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            UC.GetPicGallery.Container.Orientation = Orientation.Vertical;

            // Set style
            UC.GetPicGallery.x2.Visibility = Visibility.Visible;
            UC.GetPicGallery.Container.Margin = new Thickness(0, 60 * WindowSizing.MonitorInfo.DpiScaling, 0, 0);
            UC.GetPicGallery.border.BorderThickness = new Thickness(1, 0, 0, 0);
            UC.GetPicGallery.border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];
        }

        GalleryFunctions.ReCalculateItemSizes();
    }

    internal static void LoadBottomGallery()
    {
        UC.GetPicGallery ??= new Views.UserControls.Gallery.PicGallery();
        Panel.SetZIndex(UC.GetPicGallery, 2);
        GalleryNavigation.SetSize(Settings.Default.BottomGalleryItemSize);
        UC.GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
        UC.GetPicGallery.Height = GalleryNavigation.PicGalleryItemSize + 22;
        UC.GetPicGallery.Visibility = Visibility.Visible;
        UC.GetPicGallery.Opacity = 1;

        // Set alignment
        UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Center;
        UC.GetPicGallery.VerticalAlignment = VerticalAlignment.Bottom;

        // Set scrollbar visibility and orientation
        UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        UC.GetPicGallery.Container.Orientation = Orientation.Horizontal;

        // Set style
        UC.GetPicGallery.x2.Visibility = Visibility.Collapsed;
        UC.GetPicGallery.Container.Margin = new Thickness(0, 1, 0, 0);
        UC.GetPicGallery.border.BorderThickness = new Thickness(1);
        UC.GetPicGallery.Container.MinHeight = GalleryNavigation.PicGalleryItemSize;
        UC.GetPicGallery.border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];

        if (!ConfigureWindows.GetMainWindow.ParentContainer.Children.Contains(UC.GetPicGallery))
        {
            ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(UC.GetPicGallery);
        }
    }

    internal static async Task LoadAsync()
    {
        if (UC.GetPicGallery is null || IsLoading) { return; }

        IsLoading = true;
        var source = new CancellationTokenSource();

        var count = Navigation.Pics.Count;
        var index = Navigation.FolderIndex;
        var start = 0;
        var countChanged = false;

        for (var i = 0; i < count; i++)
        {
            try
            {
                if (count != Navigation.Pics.Count)
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

        if (count > 4000)
        {
            start = (Navigation.FolderIndex - GalleryNavigation.HorizontalItems) % Navigation.Pics.Count;
            start = start < 0 ? 0 : start;
            count = (start + GalleryNavigation.HorizontalItems) % Navigation.Pics.Count;
            countChanged = true;
        }

        await Task.Run(() =>
        {
            Parallel.For(start, count, (i, loopState) =>
            {
                try
                {
                    if (count != Navigation.Pics.Count && !countChanged || Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
                    {
                        throw new TaskCanceledException();
                    }

                    var bitmapSource = Thumbnails.GetBitmapSourceThumb(Navigation.Pics[i], (int)GalleryNavigation.PicGalleryItemSize);
                    ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() =>
                    {
                        if (i >= UC.GetPicGallery.Container.Children.Count)
                        {
                            return;
                        }
                        var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                        item.ThumbImage.Source = bitmapSource;
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
                if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 || i >= UC.GetPicGallery.Container.Children.Count)
                {
                    GalleryFunctions.Clear();
                    return;
                }

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