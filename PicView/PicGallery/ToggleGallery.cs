using PicView.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.GalleryLoad;
using static PicView.GalleryMisc;
using static PicView.GalleryScroll;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class ToggleGallery
    {
        #region Toggle 

        internal static void Toggle(bool change = false)
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

        #endregion

        #region Open

        internal static void OpenPicGalleryOne()
        {
            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            var da = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.5),
                To = 1,
                From = 0
            };

            IsOpen = true;

            picGallery.BeginAnimation(UIElement.OpacityProperty, da);           

            clickArrowLeft.Visibility =
            clickArrowRight.Visibility =
            x2.Visibility =
            minus.Visibility =
            galleryShortcut.Visibility = Visibility.Hidden;

            if (fake != null)
            {
                if (fake.grid.Children.Contains(picGallery))
                {
                    fake.grid.Children.Remove(picGallery);
                    mainWindow.bg.Children.Add(picGallery);
                }
            }

            ScrollTo();
        }

        internal static void OpenPicGalleryTwo()
        {
            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

            if (fake == null)
            {                
                fake = new FakeWindow();
            }

            if (mainWindow.bg.Children.Contains(picGallery))
            {
                mainWindow.bg.Children.Remove(picGallery);
                fake.grid.Children.Add(picGallery);
            }

            if (!fake.grid.Children.Contains(picGallery))
            {
                mainWindow.bg.Children.Remove(picGallery);
                fake.grid.Children.Add(picGallery);
            }

            fake.Show();
            IsOpen = true;
            ScrollTo();
            mainWindow.Focus();
        }

        #endregion

        #region Close

        internal static void ClosePicGalleryOne()
        {
            IsOpen = false;

            if (!Properties.Settings.Default.ShowInterface)
            {
                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility =
                galleryShortcut.Visibility = Visibility.Visible;
            }

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

        internal static void ClosePicGalleryTwo()
        {
            Properties.Settings.Default.PicGallery = 1;
            IsOpen = false;
            fake.Hide();

            Utilities.UpdateColor();

            HideInterfaceLogic.ShowStandardInterface();
        }

        #endregion

        #region Change

        internal static void ChangeToPicGalleryOne()
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

        internal static void ChangeToPicGalleryTwo()
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

        #endregion

    }
}
