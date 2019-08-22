using PicView.PreLoading;
using PicView.UserControls;
using PicView.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ImageManager;
using static PicView.Interface;
using static PicView.Navigation;
using static PicView.Fields;


namespace PicView
{
    internal static class PicGalleryLogic
    {

        #region Fields

        internal static bool LoadComplete, isLoading, IsOpen;

        internal const int picGalleryItem_Size = 230;
        internal const int picGalleryItem_Size_s = 200;

        public static int Current_page
        {
            get
            {
                return (int)Math.Floor((double)FolderIndex / Items_per_page);
            }
            set { }
        }

        public static int Total_pages
        {
            get
            {
                return (int)Math.Floor((double)Pics.Count / Items_per_page);
            }
            set { }
        }

        public static int Items_per_page
        {
            get
            {
                if (picGallery == null) return 0;
                return Properties.Settings.Default.PicGallery == 1 ?
                    Horizontal_items * Vertical_items :
                    (int)Math.Floor(picGallery.Height / picGalleryItem_Size);
            }
            set { }
        }

        public static int Horizontal_items
        {
            get
            {
                if (picGallery == null) return 0;
                return (int)Math.Floor(picGallery.Width / picGalleryItem_Size);
            }
            set { }
        }

        public static int Vertical_items
        {
            get
            {
                if (picGallery == null) return 0;
                return Properties.Settings.Default.PicGallery == 1 ?
                    (int)Math.Floor(picGallery.Height / picGalleryItem_Size) :
                    Pics.Count;
            }
            set { }
        }

        #endregion

        #region Open/Close

        internal static void PicGalleryToggle(bool change = false)
        {
            /// Quit when not enabled
            if (Properties.Settings.Default.PicGallery == 0)
                return;

            /// Toggle PicGallery, when not changed
            if (!change)
            {
                if (Properties.Settings.Default.PicGallery == 1)
                {
                    var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.5) };

                    if (!IsOpen)
                    {
                        LoadLayout();
                        da.To = 1;
                        da.From = 0;
                        IsOpen = true;
                        ScrollTo();
                    }
                    else
                    {
                        da.To = 0;
                        da.From = 1;
                        da.Completed += delegate
                        {
                            if (IsOpen && Properties.Settings.Default.PicGallery == 1)
                            {
                                picGallery.Visibility = Visibility.Collapsed;
                                IsOpen = false;
                            }
                        };
                    }

                    picGallery.BeginAnimation(UIElement.OpacityProperty, da);
                }
                else
                {
                    if (!IsOpen)
                    {
                        LoadLayout();

                        if (Properties.Settings.Default.ShowInterface)
                            HideInterface();

                        if (fake != null)
                        {
                            if (!fake.grid.Children.Contains(picGallery))
                            {
                                mainWindow.bg.Children.Remove(picGallery);
                                fake.grid.Children.Add(picGallery);
                            }
                        }
                        else
                        {
                            mainWindow.bg.Children.Remove(picGallery);
                            fake = new FakeWindow();
                            fake.grid.Children.Add(picGallery);
                        }

                        fake.Show();
                        IsOpen = true;
                        ScrollTo();
                    }
                    else
                    {
                        fake.Hide();
                        IsOpen = false;
                    }

                }
            }
            /// Toggle PicGallery, when changed
            else
            {
                if (Properties.Settings.Default.PicGallery == 1)
                {
                    Properties.Settings.Default.PicGallery = 2;
                    LoadLayout();

                    if (fake == null)
                        fake = new FakeWindow();

                    if (!fake.grid.Children.Contains(picGallery))
                    {
                        mainWindow.bg.Children.Remove(picGallery);
                        fake.grid.Children.Add(picGallery);
                    }

                    fake.Show();
                }
                else
                {
                    fake.grid.Children.Remove(picGallery);
                    fake.Hide();
                    mainWindow.bg.Children.Add(picGallery);

                    Properties.Settings.Default.PicGallery = 1;
                    LoadLayout();
                }
            }
        }

        #endregion

        #region Load PicGallery

        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            picGallery.Scroller.PreviewMouseWheel += Scroller_MouseWheel;
            picGallery.Scroller.MouseDown += (s, x) => mainWindow.Focus();
            picGallery.x2.MouseLeftButtonUp += (s, x) =>
            {
                if (Properties.Settings.Default.PicGallery == 1)
                    FadeOut();
            };
            picGallery.grid.MouseLeftButtonDown += (s, x) => mainWindow.Focus();

            LoadComplete = isLoading = IsOpen = false;
        }

        internal static void LoadLayout()
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.ShowInterface)
                {
                    picGallery.Width = Application.Current.MainWindow.Width - 15;
                    picGallery.Height = Application.Current.MainWindow.ActualHeight - 78;
                }
                else
                {
                    picGallery.Width = Application.Current.MainWindow.Width - 2;
                    picGallery.Height = Application.Current.MainWindow.Height - 2; // 2px for borders
                }

                picGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                picGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                picGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                picGallery.x2.Visibility = Visibility.Visible;
                picGallery.Container.Margin = new Thickness(0, 65, 0, 0);
            }
            else
            {
                picGallery.Width = picGalleryItem_Size + 19; // 17 for scrollbar width + 2 for borders
                picGallery.Height = MonitorInfo.Height;
                picGallery.HorizontalAlignment = HorizontalAlignment.Right;
                picGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                picGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                picGallery.x2.Visibility = Visibility.Collapsed;
                picGallery.Container.Margin = new Thickness(0, 0, 0, 0);
            }

            picGallery.Visibility = Visibility.Visible;
            picGallery.Opacity = 1;
            picGallery.Container.Orientation = Orientation.Vertical;
        }

        internal static Task Load()
        {
            isLoading = true;
            return Task.Run(() =>
            {
                for (int i = 0; i < Pics.Count; i++)
                {
                    var pic = GetBitmapSourceThumb(Pics[i]);
                    pic.Freeze();
                    Add(pic, i);
                }
                LoadComplete = true;
                isLoading = false;
            });
        }

        #endregion

        #region Main Functions

        private static async void Add(BitmapSource pic, int index)
        {
            await mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var selected = index == FolderIndex;
                var item = new PicGalleryItem(pic, index, selected);
                item.MouseLeftButtonUp += (s, x) =>
                {
                    Click(index);

                    if (!selected && FolderIndex < picGallery.Container.Children.Count)
                    {
                        item.Setselected(true);
                        var child = picGallery.Container.Children[FolderIndex] as PicGalleryItem;
                        child.Setselected(false);
                    }

                };
                picGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            LoadComplete = isLoading = IsOpen = false;
            picGallery.Container.Children.Clear();
        }

        //internal static void Sort()
        //{
        //    var x = picGallery.Container.Children.Cast<PicGalleryItem>();
        //}

        private static void FadeOut()
        {
            var da = new DoubleAnimation()
            {
                To = 0,
                From = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };
            da.Completed += delegate
            {
                picGallery.Visibility = Visibility.Collapsed;
            };
            picGallery.BeginAnimation(UIElement.OpacityProperty, da);
        }

        #endregion

        #region Scroll

        private static void Scroller_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                ScrollTo(e.Delta > 0);
            else
                ScrollTo(e.Delta > 0, false);
        }

        /// <summary>
        /// Scrolls a page back or forth
        /// </summary>
        /// <param name="next"></param>
        /// <param name="end"></param>
        internal static void ScrollTo(bool next, bool end = false, bool speedUp = false, bool animate = false)
        {
            if (end)
            {
                if (next)
                    picGallery.Scroller.ScrollToRightEnd();
                else
                    picGallery.Scroller.ScrollToLeftEnd();
            }
            else
            {
                var speed = speedUp ? picGalleryItem_Size * 4.7 : picGalleryItem_Size;
                var direction = next ? picGallery.Scroller.HorizontalOffset - speed : picGallery.Scroller.HorizontalOffset + speed;

                if (Properties.Settings.Default.PicGallery == 1)
                {
                    if (animate)
                    {
                        //var anim = new DoubleAnimation
                        //{
                        //    From = Scroller.HorizontalOffset,
                        //    To = direction,
                        //    Duration = TimeSpan.FromSeconds(.3),
                        //    AccelerationRatio = 0.2,
                        //    DecelerationRatio = 0.4
                        //};

                        //var sb = new Storyboard();
                        //sb.Children.Add(anim);
                        //Storyboard.SetTarget(anim, Scroller);
                        //Storyboard.SetTargetProperty(anim, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty))
                    }
                    else
                    {
                        picGallery.Scroller.ScrollToHorizontalOffset(direction);
                    }
                }
                else
                {
                    if (animate)
                    {
                        //DoubleAnimation verticalAnimation = new DoubleAnimation
                        //{
                        //    From = scrollViewer.VerticalOffset,
                        //    To = some
                        //};
                        //value;
                        //verticalAnimation.Duration = new Duration(some duration);

                        //Storyboard storyboard = new Storyboard();

                        //storyboard.Children.Add(verticalAnimation);
                        //Storyboard.SetTarget(verticalAnimation, scrollViewer);
                        //Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty)); // Attached dependency property
                        //storyboard.Begin();
                    }
                    else
                    {
                        if (next)
                            picGallery.Scroller.ScrollToVerticalOffset(picGallery.Scroller.VerticalOffset - speed);
                        else
                            picGallery.Scroller.ScrollToVerticalOffset(picGallery.Scroller.VerticalOffset + speed);
                    }
                }
            }
        }

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        /// <param name="item">The index of picGalleryItem</param>
        internal static void ScrollTo()
        {
            if (Properties.Settings.Default.PicGallery == 1)
                picGallery.Scroller.ScrollToHorizontalOffset((picGalleryItem_Size * Horizontal_items) * Current_page);
            else
                picGallery.Scroller.ScrollToVerticalOffset((picGalleryItem_Size * Items_per_page) * Current_page);
        }

        #endregion Scroll

        #region Item Click

        internal static void Click(int id)
        {
            mainWindow.Focus();


            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Preloader.Contains(Pics[id]))
                    PreviewItemClick(Preloader.Load(Pics[id]), id);
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
                if (Preloader.Contains(Pics[id]))
                {
                    ItemClick(id);
                }
                else
                {
                    ItemClick(id);
                }

            }

            IsOpen = false;
        }

        internal static void PreviewItemClick(ImageSource source, int id)
        {
            mainWindow.img.Source = source;
            var size = ImageSize(Pics[id]);
            if (size.HasValue)
                Resize_and_Zoom.ZoomFit(size.Value.Width, size.Value.Height);
        }

        internal static void ItemClick(int id)
        {
            Pic(id);

            if (Properties.Settings.Default.PicGallery == 1)
                picGallery.Visibility = Visibility.Collapsed;

            /// Fix annoying bug
            ajaxLoading.Opacity = 0;
        }

        #endregion

    }
}
