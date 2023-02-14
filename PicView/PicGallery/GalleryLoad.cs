using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.UILogic.HideInterfaceLogic;

namespace PicView.PicGallery
{
    internal static class GalleryLoad
    {
        internal static bool IsLoading { get; private set; }

        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields, when it's loaded.
            UC.GetPicGallery.Scroller.PreviewMouseWheel += GalleryNavigation.ScrollTo;
            UC.GetPicGallery.Scroller.ScrollChanged += (s, x) => ConfigureWindows.GetMainWindow.Focus(); // Maintain window focus when scrolling manually
            UC.GetPicGallery.grid.MouseLeftButtonDown += (s, x) => ConfigureWindows.GetMainWindow.Focus();
            UC.GetPicGallery.x2.MouseLeftButtonDown += delegate { GalleryToggle.CloseHorizontalGallery(); };
        }

        internal static void LoadLayout(bool fullscreen)
        {
            if (UC.GetPicGallery == null)
            {
                UC.GetPicGallery = new Views.UserControls.Gallery.PicGallery
                {
                    Opacity = 0
                };

                ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(UC.GetPicGallery);
                Panel.SetZIndex(UC.GetPicGallery, 999);
            }

            if (fullscreen)
            {
                WindowSizing.RenderFullscreen();

                if (Settings.Default.FullscreenGalleryHorizontal)
                {
                    // Set size
                    GalleryNavigation.SetSize(35);
                    UC.GetPicGallery.Width = WindowSizing.MonitorInfo.WorkArea.Width;

                    // Set alignment
                    UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Center;
                    UC.GetPicGallery.VerticalAlignment = VerticalAlignment.Bottom;

                    // Set scrollbar visibility and orientation
                    UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    UC.GetPicGallery.Container.Orientation = Orientation.Horizontal;

                    // Set style
                    UC.GetPicGallery.Margin = new Thickness(0);
                    UC.GetPicGallery.border.BorderThickness = new Thickness(1);

                    // Make sure bools are correct
                    GalleryFunctions.IsHorizontalOpen = false;
                    GalleryFunctions.IsHorizontalFullscreenOpen = true;
                    GalleryFunctions.IsVerticalFullscreenOpen = false;
                }
                else
                {
                    // Set size
                    GalleryNavigation.SetSize(22);
                    UC.GetPicGallery.Width = (GalleryNavigation.PicGalleryItem_Size + 25) * WindowSizing.MonitorInfo.DpiScaling;
                    UC.GetPicGallery.Height = WindowSizing.MonitorInfo.WorkArea.Height;

                    // Set alignment
                    UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Right;
                    UC.GetPicGallery.VerticalAlignment = VerticalAlignment.Stretch;

                    // Set scrollbar visibility and orientation
                    UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    UC.GetPicGallery.Container.Orientation = Orientation.Vertical;

                    // Set style
                    UC.GetPicGallery.Margin = new Thickness(0, 0, 0, 0);
                    UC.GetPicGallery.border.BorderThickness = new Thickness(1, 0, 0, 0);
                    if (Settings.Default.DarkTheme)
                    {
                        UC.GetPicGallery.border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];
                    }
                    else
                    {
                        UC.GetPicGallery.border.Background = new SolidColorBrush(Colors.Transparent);
                    }

                    // Make sure bools are correct
                    GalleryFunctions.IsHorizontalOpen = false;
                    GalleryFunctions.IsHorizontalFullscreenOpen = false;
                    GalleryFunctions.IsVerticalFullscreenOpen = true;
                }

                UC.GetPicGallery.x2.Visibility = Visibility.Collapsed;

                ShowNavigation(false);
                ShowTopandBottom(false);
                ConfigureWindows.GetMainWindow.Focus();
            }
            else
            {
                // Set size
                GalleryNavigation.SetSize(17);

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
                UC.GetPicGallery.Container.Margin = new Thickness(0, 65 * WindowSizing.MonitorInfo.DpiScaling, 0, 0);
                UC.GetPicGallery.border.BorderThickness = new Thickness(1, 0, 0, 0);
                UC.GetPicGallery.border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];

                // Make sure booleans are correct
                GalleryFunctions.IsHorizontalOpen = true;
                GalleryFunctions.IsHorizontalFullscreenOpen = false;
                GalleryFunctions.IsVerticalFullscreenOpen = false;
            }

            if (UC.GetPicGallery.Container.Children.Count <= 0) { return; }
            var tempItem = (PicGalleryItem)UC.GetPicGallery.Container.Children[0];

            if (Math.Abs(tempItem.outterborder.Height - GalleryNavigation.PicGalleryItem_Size) < 1) { return; }

            for (int i = 0; i < UC.GetPicGallery.Container.Children.Count; i++)
            {
                var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                item.innerborder.Height = GalleryNavigation.PicGalleryItem_Size_s;
            }
        }

        internal static async Task Load()
        {
            IsLoading = true;
            var source = new CancellationTokenSource();
            var task = Task.Run(() => LoopAsync(source.Token), source.Token);
            try
            {
                await task.ConfigureAwait(false);
                IsLoading = false;
            }
            catch (TaskCanceledException)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(async () =>
                {
                    UC.GetPicGallery.Container.Children.Clear();
                    await Load().ConfigureAwait(false); // restart when changing directory
                }));
            }
            finally { source.Dispose(); }
        }

        private static async Task LoopAsync(CancellationToken cancellationToken)
        {
            if (UC.GetPicGallery is null) { return; }

            var count = Navigation.Pics.Count;
            var index = Navigation.FolderIndex;

            for (int i = 0; i < count; i++)
            {
                if (count != Navigation.Pics.Count)
                {
                    throw new TaskCanceledException();
                }

                Add(i, index);
                await UpdatePic(i).ConfigureAwait(false);
            }
        }

        internal static void Add(int i, int index)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                var selected = i == index;
                var item = new PicGalleryItem(null, i, selected);
                item.MouseLeftButtonDown += async delegate
                {
                    await GalleryClick.ClickAsync(i).ConfigureAwait(false);
                };

                UC.GetPicGallery.Container.Children.Add(item);

                if (!selected) { return; }
                GalleryNavigation.SelectedGalleryItem = i;
                Timers.PicGalleryTimerHack();
            }));
        }

        internal static async Task UpdatePic(int i)
        {
            if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1)
            {
                GalleryFunctions.Clear();
                await Load().ConfigureAwait(false); // restart when changing directory
                return;
            }

            var pic = await Task.FromResult(
                Thumbnails.GetBitmapSourceThumb(new FileInfo(Navigation.Pics[i]), (int)GalleryNavigation.PicGalleryItem_Size)
                ?? ImageFunctions.ImageErrorMessage());
            UpdatePic(i, pic);
        }

        internal static void UpdatePic(int i, BitmapSource pic)
        {
            try
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
                {
                    if (Navigation.Pics?.Count < Navigation.FolderIndex || Navigation.Pics?.Count < 1 || i >= UC.GetPicGallery.Container.Children.Count)
                    {
                        GalleryFunctions.Clear();
                        Load().ConfigureAwait(false); // restart when changing directory
                        return;
                    }

                    var item = (PicGalleryItem)UC.GetPicGallery.Container.Children[i];
                    item.img.Source = pic;
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
}