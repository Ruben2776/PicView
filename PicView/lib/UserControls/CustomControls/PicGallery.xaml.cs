using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.lib.ImageManager;
using static PicView.lib.Variables;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for PicGallery.xaml
    /// </summary>
    public partial class PicGallery : UserControl
    {
        #region Constructor, variables, loaded

        public bool LoadComplete, isLoading, open;

        public event MyEventHandler PreviewItemClick, ItemClick;

        private int current_page, total_pages, items_per_page, horizontal_items, vertical_items;

        const int picGalleryItem_Size = 230;

        //int next_page
        //{
        //    get {
        //        return current_page + 1 > total_pages ? total_pages : current_page + 1;
        //    }
        //}

        //int prev_page
        //{
        //    get
        //    {
        //        return current_page - 1 < 0 ? 1 : current_page - 1;
        //    }
        //}

        public PicGallery()
        {
            InitializeComponent();
            Loaded += PicGallery_Loaded;
        }

        private void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            Scroller.PreviewMouseWheel += Scroller_MouseWheel;
            Scroller.MouseDown += (s,x) => Application.Current.MainWindow.Focus();
            x2.MouseLeftButtonUp += X2_MouseLeftButtonUp;
            grid.MouseLeftButtonDown += (s,x) => Application.Current.MainWindow.Focus();
            
            LoadComplete = isLoading = open = false;
        }

        #endregion

        #region animations

        private void X2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Properties.Settings.Default.PicGallery == 1)
                FadeOut();
        }

        private void FadeOut()
        {
            var da = new DoubleAnimation()
            {
                To = 0,
                From = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };
            da.Completed += delegate
            {
                open = false;
                Visibility = Visibility.Collapsed;
            };
            BeginAnimation(OpacityProperty, da);
        }

        #endregion

        internal void Calculate_Paging()
        {
            if (Container.ActualHeight == 0)
                return;

            if (Properties.Settings.Default.PicGallery == 1)
            {
                horizontal_items = (int)Math.Ceiling(Width / picGalleryItem_Size);
                vertical_items = (int)Math.Ceiling(Container.ActualHeight / picGalleryItem_Size);
                items_per_page = horizontal_items * vertical_items;
                total_pages = (int)Math.Floor((double)Pics.Count / items_per_page);
                current_page = (int)Math.Floor((double)FolderIndex / items_per_page);
                return;
            }

            vertical_items = Pics.Count;
            items_per_page = (int)Math.Floor(Height / picGalleryItem_Size);
            total_pages = (int)Math.Floor((double)Pics.Count / items_per_page);
            current_page = (int)Math.Floor((double)FolderIndex / items_per_page);
        }

        private async void Add(BitmapSource pic, int index)
        {
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var item = new PicGalleryItem(pic);
                item.MouseLeftButtonUp += (s, x) => Click(index);
                Container.Children.Add(item);
            }));
        }

        internal void Load()
        {
            isLoading = true;
            var t = new Task(() =>
            {
                for (int i = 0; i < Pics.Count; i++)
                {
                    var pic = GetBitmapSourceThumb(Pics[i]);
                    if (pic != null)
                    {
                        pic.Freeze();
                        Add(pic, i);
                    }

                    if (i == Pics.Count - 1)
                    {
                        LoadComplete = true;
                        isLoading = false;
                    }
                }
            });
            t.Start();
        }

        internal void Clear()
        {
            LoadComplete = isLoading = open = false;
            Container.Children.Clear();
        }

        internal void Sort()
        {
            var x = Container.Children.Cast<PicGalleryItem>();
        }

        internal void Click(int id)
        {
            Application.Current.MainWindow.Focus();
            PreviewItemClick(this, new MyEventArgs(id, null));

            if (Properties.Settings.Default.PicGallery == 1)
            {
                var img = new Image()
                {
                    Source = GetBitmapSourceThumb(Pics[id]),
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var border = new Border()
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"],
                    Background = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"]
                };
                border.Child = img;
                grid.Children.Add(border);

                var from = picGalleryItem_Size;
                var to = new double[] { Application.Current.MainWindow.ActualWidth - 15, Application.Current.MainWindow.ActualHeight - 95 };

                var da = new DoubleAnimation
                {
                    From = from,
                    To = to[0],
                    Duration = TimeSpan.FromSeconds(.3),
                    AccelerationRatio = 0.2,
                    DecelerationRatio = 0.4
                };

                var da0 = new DoubleAnimation
                {
                    From = from,
                    To = to[1],
                    Duration = TimeSpan.FromSeconds(.3),
                    AccelerationRatio = 0.2,
                    DecelerationRatio = 0.4
                };

                da.Completed += delegate
                {
                    ItemClick(this, new MyEventArgs(id, img.Source));
                    grid.Children.Remove(border);
                    Visibility = Visibility.Collapsed;
                    picGallery.open = false;
                };

                img.BeginAnimation(WidthProperty, da);
                img.BeginAnimation(HeightProperty, da0);
            }
            else
            {
                ItemClick(this, new MyEventArgs(id, GetBitmapSourceThumb(Pics[id])));
            }
            Application.Current.MainWindow.Focus();
        }

        #region Scroll

        private void Scroller_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                ScrollTo(e.Delta > 0, true);
            else
                ScrollTo(e.Delta > 0);
        }

        /// <summary>
        /// Scrolls a page back or forth
        /// </summary>
        /// <param name="next"></param>
        /// <param name="end"></param>
        internal void ScrollTo(bool next, bool end = false, bool animate = false)
        {
            if (end)
            {
                if (next)
                    Scroller.ScrollToRightEnd();
                else
                    Scroller.ScrollToLeftEnd();
            }
            else
            {
                var direction = next ? Scroller.HorizontalOffset + picGalleryItem_Size : Scroller.HorizontalOffset - picGalleryItem_Size;

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
                        if (next)
                            Scroller.ScrollToHorizontalOffset(direction);
                        else
                            Scroller.ScrollToHorizontalOffset(direction);
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
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - picGalleryItem_Size); 
                        else
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + picGalleryItem_Size);
                    }
                }
            }
        }

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        /// <param name="item">The index of picGalleryItem</param>
        internal void ScrollTo()
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                var x = (picGalleryItem_Size * horizontal_items) * current_page;
                Scroller.ScrollToHorizontalOffset(x);
                return;
            }

            if (vertical_items != 0)
            {
                var y = (picGalleryItem_Size * Pics.Count) * current_page;
                Scroller.ScrollToVerticalOffset(y);
            }
        }

        #endregion Scroll
    }

    // Event

    public delegate void MyEventHandler(object source, MyEventArgs e);

    public class MyEventArgs : EventArgs
    {
        private int Id;
        private ImageSource img;

        public MyEventArgs(int Id, ImageSource img)
        {
            this.Id = Id;
            this.img = img;
        }

        public int GetId()
        {
            return Id;
        }

        public ImageSource GetImage()
        {
            return img;
        }
    }
}