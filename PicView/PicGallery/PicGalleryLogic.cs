using PicView.PreLoading;
using PicView.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.Fields;
using static PicView.ImageDecoder;
using static PicView.Navigation;
using static PicView.Thumbnails;


namespace PicView
{
    internal static class PicGalleryLogic
    {
        internal static bool IsLoading;

        internal static volatile bool IsOpen;

        internal static async void Add(BitmapSource pic, int index)
        {
            await mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var selected = index == FolderIndex;
                var item = new PicGalleryItem(pic, index, selected);
                item.MouseLeftButtonUp += (s, x) =>
                {
                    var child = picGallery.Container.Children[FolderIndex] as PicGalleryItem;
                    child.SetSelected(false);
                    Click(index);
                    item.SetSelected(true);
                };
                picGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            IsLoading = IsOpen = false;
            picGallery.Container.Children.Clear();
        }

        //internal static void Sort()
        //{
        //    // TODO Get PicGallery sorting working
        //    var items = picGallery.Container.Children.Cast<PicGalleryItem>();
        //    switch (Properties.Settings.Default.SortPreference)
        //    {
        //        // Alphanumeric sort
        //        case 0:
        //            var list = items.ToList();
        //            list.Sort((x, y) => { return NativeMethods.StrCmpLogicalW(x, y); });
        //            break;
        //        case 1:
        //            items = items.OrderBy(f => new FileInfo(f).Length);
        //            break;
        //        case 2:
        //            items = items.OrderBy(f => new FileInfo(f).Extension);
        //            break;
        //        case 3:
        //            items = items.OrderBy(f => new FileInfo(f).CreationTime);
        //            break;
        //        case 4:
        //            items = items.OrderBy(f => new FileInfo(f).LastAccessTime);
        //            break;
        //        case SortFilesBy.Lastwritetime:
        //            items = items.OrderBy(f => new FileInfo(f).LastWriteTime);
        //            break;
        //        case SortFilesBy.Random:
        //            items = items.OrderBy(f => Guid.NewGuid());
        //            break;
        //    }
        //}

        internal static void Click(int id)
        {
            mainWindow.Focus();


            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Preloader.Contains(Pics[id]))
                {
                    PreviewItemClick(Preloader.Load(Pics[id]), id);
                }
                else
                {
                    var z = picGallery.Container.Children[id] as PicGalleryItem;
                    PreviewItemClick(z.img.Source, id);
                }

                picGallery.Width = xWidth;
                picGallery.Height = xHeight;

                var img = new Image()
                {
                    Source = GetBitmapSourceThumb(Pics[id]),
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // Need to add border for background to pictures with transparent background
                var border = new Border()
                {
                    Background = Properties.Settings.Default.BgColorWhite ? new SolidColorBrush(Colors.White) : Application.Current.Resources["BackgroundColorBrush"] as SolidColorBrush
                };
                border.Child = img;
                picGallery.grid.Children.Add(border);

                var from = picGalleryItem_Size;
                var to = new double[] { xWidth, xHeight };
                var acceleration = 0.2;
                var deceleration = 0.4;
                var duration = TimeSpan.FromSeconds(.3);

                var da = new DoubleAnimation
                {
                    From = from,
                    To = to[0],
                    Duration = duration,
                    AccelerationRatio = acceleration,
                    DecelerationRatio = deceleration
                };

                var da0 = new DoubleAnimation
                {
                    From = from,
                    To = to[1],
                    Duration = duration,
                    AccelerationRatio = acceleration,
                    DecelerationRatio = deceleration
                };

                da.Completed += delegate
                {
                    ItemClick(id);
                    picGallery.grid.Children.Remove(border);
                    IsOpen = false;
                };

                border.BeginAnimation(FrameworkElement.WidthProperty, da);
                border.BeginAnimation(FrameworkElement.HeightProperty, da0);
            }
            else
            {
                if (!Preloader.Contains(Pics[id]))
                {
                    PreloadCount = 4;
                    Preloader.Clear();
                    Preloader.Add(Pics[id]);
                }

                ItemClick(id);
            }

            IsOpen = false;
        }

        internal static void PreviewItemClick(ImageSource source, int id)
        {
            if (!Preloader.Contains(Pics[id]))
            {
                PreloadCount = 4;
                Preloader.Clear();
                Preloader.Add(Pics[id]);
            }

            mainWindow.img.Source = source;
            var size = ImageSize(Pics[id]);
            if (size.HasValue)
            {
                Resize_and_Zoom.ZoomFit(size.Value.Width, size.Value.Height);
            }
        }

        internal static void ItemClick(int id)
        {
            Pic(id);

            if (Properties.Settings.Default.PicGallery == 1)
            {
                picGallery.Visibility = Visibility.Collapsed;
            }
        }

    }
}
