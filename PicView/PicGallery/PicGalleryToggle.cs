using PicView.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.PicGalleryLoad;
using static PicView.PicGalleryLogic;
using static PicView.PicGalleryScroll;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class PicGalleryToggle
    {
        internal static void ToggleGallery(bool change = false)
        {
            /// TODO need to get this fixed when changing between them.
            /// Is open variable stats true when it should be false, dunno why..

            var picGallery = Properties.Settings.Default.PicGallery;

            /// Quit when not enabled
            if (picGallery == 0)
            {
                return;
            }

            /// Toggle PicGallery, when not changed
            if (!change)
            {
                if (picGallery == 1)
                {
                    if (!IsOpen)
                    {
                        OpenPicGalleryOne();
                    }
                    else
                    {
                        ClosePicGalleryOne();
                    }
                }
                else
                {
                    if (!IsOpen)
                    {
                        OpenPicGalleryTwo();
                    }
                    else
                    {
                        ClosePicGalleryTwo();
                    }
                }
            }
            /// Toggle PicGallery, when changed
            else
            {
                if (picGallery == 1)
                {
                    ChangeToPicGalleryTwo();
                }
                else
                {
                    ChangeToPicGalleryOne();
                }
            }

#if DEBUG
            Trace.WriteLine(nameof(picGallery) + " " + picGallery + " IsOpen = " + IsOpen);
#endif
        }

        // Open!!!

        static void OpenPicGalleryOne()
        {
            LoadLayout();

            var da = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.5),
                To = 1,
                From = 0
            };

            IsOpen = true;

            picGallery.BeginAnimation(UIElement.OpacityProperty, da);
            ScrollTo();
        }

        static void OpenPicGalleryTwo()
        {
            LoadLayout();

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
            mainWindow.Focus();
        }

        // Close!!!

        static void ClosePicGalleryOne()
        {
            IsOpen = false;
            var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.5) };

            da.To = 0;
            da.From = 1;
            da.Completed += delegate
            {
                //if (IsOpen && Properties.Settings.Default.PicGallery == 1)
                //{
                //    picGallery.Visibility = Visibility.Collapsed;
                //}
                picGallery.Visibility = Visibility.Collapsed;
            };

            picGallery.BeginAnimation(UIElement.OpacityProperty, da);
        }

        static void ClosePicGalleryTwo()
        {
            IsOpen = false;
            fake.Hide();

            Helper.UpdateColor();

            if (!Properties.Settings.Default.ShowInterface)
            {
                HideInterfaceLogic.ShowNavigation(true);
            }

            //// Don't show it on next startup
            //Properties.Settings.Default.PicGallery = 1;
        }


        // Change !!

        static void ChangeToPicGalleryOne()
        {
            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            if (fake.grid.Children.Contains(picGallery))
            {
                fake.grid.Children.Remove(picGallery);
                mainWindow.bg.Children.Add(picGallery);
            }

            fake.Hide();
        }

        static void ChangeToPicGalleryTwo()
        {
            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

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
            ScrollTo();
            mainWindow.Focus();
        }

    }
}
