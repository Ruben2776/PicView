using PicView.PreLoading;
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
using static PicView.Variables;
using static PicView.ImageManager;

namespace PicView.UserControls
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
            Scroller.MouseDown += (s,x) => mainWindow.Focus();
            x2.MouseLeftButtonUp += X2_MouseLeftButtonUp;
            grid.MouseLeftButtonDown += (s,x) => mainWindow.Focus();
            
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
            if (Properties.Settings.Default.PicGallery == 1)
            {
                horizontal_items = (int)Math.Ceiling(Width / PicGalleryItem.picGalleryItem_Size);
                vertical_items = (int)Math.Ceiling(Height / PicGalleryItem.picGalleryItem_Size);
                items_per_page = horizontal_items * vertical_items;
            }
            else
                items_per_page = (int)Math.Floor(Height / PicGalleryItem.picGalleryItem_Size);

            total_pages = (int)Math.Ceiling((double)Pics.Count / items_per_page);
            current_page = (int)Math.Floor((double)FolderIndex / items_per_page);
        }

        internal void LoadLayout()
        {
            Calculate_Paging();

            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.ShowInterface)
                {
                    Width = Application.Current.MainWindow.Width - 15;
                    Height = Application.Current.MainWindow.ActualHeight - 85;
                }
                else
                {
                    Width = Application.Current.MainWindow.Width - 2;
                    Height = Application.Current.MainWindow.Height - 2; // 2px for borders
                }

                HorizontalAlignment = HorizontalAlignment.Stretch;
                Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                x2.Visibility = Visibility.Visible;
            }
            else
            {
                Width = PicGalleryItem.picGalleryItem_Size + 19; // 17 for scrollbar width + 2 for borders
                Height = MonitorInfo.Height;
                HorizontalAlignment = HorizontalAlignment.Right;
                Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                x2.Visibility = Visibility.Collapsed;
            }

            Visibility = Visibility.Visible;
            Opacity = 1;
            Container.Orientation = Orientation.Vertical;
        }

        private async void Add(BitmapSource pic, int index)
        {
            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var selected = index == FolderIndex;
                var item = new PicGalleryItem(pic, index, selected);
                item.MouseLeftButtonUp += (s, x) => 
                {
                    Click(index);

                    if (!selected && FolderIndex < Container.Children.Count)
                    {
                        item.Setselected(true);
                        var child = Container.Children[FolderIndex] as PicGalleryItem;
                        child.Setselected(false);
                    }

                };
                Container.Children.Add(item);
            }));
        }

        internal async void Load()
        {
            isLoading = true;
            await Task.Run(() =>
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
            mainWindow.Focus();


            if (Properties.Settings.Default.PicGallery == 1)
            {
                var z = Container.Children[id] as PicGalleryItem;
                PreviewItemClick(this, new MyEventArgs(id, z.img.Source));
                Width = xWidth;
                Height = xHeight;

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
                grid.Children.Add(border);

                var from = PicGalleryItem.picGalleryItem_Size;
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
                    ItemClick(this, new MyEventArgs(id, img.Source));
                    grid.Children.Remove(border);
                    picGallery.open = false;
                };

                border.BeginAnimation(WidthProperty, da);
                border.BeginAnimation(HeightProperty, da0);
            }
            else
            {
                if (Preloader.Contains(Pics[id]))
                {
                    ItemClick(this, new MyEventArgs(id, null));
                }
                else
                {
                    var z = Container.Children[id] as PicGalleryItem;
                    PreviewItemClick(this, new MyEventArgs(id, z.img.Source));
                    ItemClick(this, new MyEventArgs(id, null));
                }

            }

            picGallery.open = false;
        }

        #region Scroll

        private void Scroller_MouseWheel(object sender, MouseWheelEventArgs e)
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
        internal void ScrollTo(bool next, bool end = false, bool speedUp = false, bool animate = false)
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
                var speed = speedUp ? PicGalleryItem.picGalleryItem_Size * 4.7 : PicGalleryItem.picGalleryItem_Size;
                var direction = next ? Scroller.HorizontalOffset - speed : Scroller.HorizontalOffset + speed;

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
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - speed); 
                        else
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + speed);
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
                Scroller.ScrollToHorizontalOffset((PicGalleryItem.picGalleryItem_Size * horizontal_items) * current_page);
            else
                Scroller.ScrollToVerticalOffset((PicGalleryItem.picGalleryItem_Size * items_per_page) * current_page);
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