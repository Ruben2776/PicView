using PicView.PreLoading;
using PicView.UserControls;
using PicView.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.Variables;
using static PicView.ImageManager;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;
using static PicView.Interface;

namespace PicView
{
    internal static class PicGallery
    {
        #region PicGallery

        // PicGallery

        internal static void PicGalleryFade(bool show = true)
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.5) };
                if (show)
                {
                    da.To = 1;
                    da.From = 0;

                    picGallery.open = true;
                    picGallery.LoadLayout();
                    picGallery.ScrollTo();

                    if (Application.Current.Windows.OfType<FakeWindow>().Any())
                    {
                        var fake = Application.Current.Windows[1] as FakeWindow;
                        fake.grid.Children.Remove(picGallery);
                        fake.Hide();
                        if (!mainWindow.bg.Children.Contains(picGallery))
                            mainWindow.bg.Children.Add(picGallery);
                    }
                }
                else
                {
                    da.To = 0;
                    da.From = 1;
                    da.Completed += delegate
                    {
                        picGallery.Visibility = Visibility.Collapsed;
                        picGallery.open = false;
                    };
                }

                picGallery.BeginAnimation(UIElement.OpacityProperty, da);
            }

            else if (Properties.Settings.Default.PicGallery == 2)
            {

                if (Properties.Settings.Default.ShowInterface)
                    HideInterface();

                if (show)
                {

                    if (Application.Current.Windows.OfType<FakeWindow>().Any())
                    {
                        var f = Application.Current.Windows[1] as FakeWindow;

                        if (!f.grid.Children.Contains(picGallery))
                        {
                            mainWindow.bg.Children.Remove(picGallery);
                            f.grid.Children.Add(picGallery);
                        }

                        f.Show();
                        picGallery.open = true;
                        picGallery.Visibility = Visibility.Visible;
                        return;
                    }

                    mainWindow.bg.Children.Remove(picGallery);

                    picGallery.LoadLayout();
                    picGallery.Calculate_Paging();
                    picGallery.ScrollTo();

                    var fake = new FakeWindow();

                    fake.grid.Children.Add(picGallery);
                    fake.Show();
                    picGallery.open = true;

                    return;
                }

                if (Application.Current.Windows.OfType<FakeWindow>().Any())
                    Application.Current.Windows[1].Hide();

                picGallery.Visibility = Visibility.Collapsed;
                picGallery.open = false;
            }
        }

        internal static async void PicGallery_PreviewItemClick(object source, MyEventArgs e)
        {
            mainWindow.img.Source = e.GetImage();
            var size = ImageSize(Pics[e.GetId()]);
            if (size.HasValue)
                ZoomFit(size.Value.Width, size.Value.Height);

            await System.Threading.Tasks.Task.Run(() =>
            {
                //Preloader.Clear();
                Preloader.Add(e.GetId());
            });
        }

        internal static void PicGallery_ItemClick(object source, MyEventArgs e)
        {
            mainWindow.Bar.Text = Loading;
            Pic(e.GetId());

            if (Properties.Settings.Default.PicGallery == 1)
                picGallery.Visibility = Visibility.Collapsed;

            /// Fix annoying bug
            ajaxLoading.Opacity = 0;
        }

        #endregion
    }
}
