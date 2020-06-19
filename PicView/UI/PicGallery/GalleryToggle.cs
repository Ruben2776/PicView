using PicView.Library;
using PicView.UI.Sizing;
using PicView.UI.Windows;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.Library.Fields;
using static PicView.UI.PicGallery.GalleryFunctions;
using static PicView.UI.PicGallery.GalleryLoad;
using static PicView.UI.PicGallery.GalleryScroll;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.PicGallery
{
    internal static class GalleryToggle
    {
        #region Toggle 

        internal static void Toggle(bool change = false)
        {
            if (Pics.Count < 1)
            {
                return;
            }

            var picGallery = Properties.Settings.Default.PicGallery;

            /// Toggle PicGallery, when not changed
            if (!change)
            {
                if (picGallery == 1)
                {
                    if (!IsOpen)
                    {
                        OpenContainedGallery();
                    }
                    else
                    {
                        CloseContainedGallery();
                    }
                }
                else
                {
                    if (!IsOpen)
                    {
                        OpenFullscreenGallery();
                    }
                    else
                    {
                        CloseFullscreenGallery();
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
        }

        #endregion

        #region Open

        internal static async void OpenContainedGallery()
        {
            if (Pics.Count < 1)
            {
                return;
            }

            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            var da = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.5),
                To = 1,
                From = 0
            };

            picGallery.BeginAnimation(UIElement.OpacityProperty, da);           

            clickArrowLeft.Visibility =
            clickArrowRight.Visibility =
            x2.Visibility =
            minus.Visibility =
            galleryShortcut.Visibility = Visibility.Hidden;

            if (fakeWindow != null)
            {
                if (fakeWindow.grid.Children.Contains(picGallery))
                {
                    fakeWindow.grid.Children.Remove(picGallery);
                    mainWindow.bg.Children.Add(picGallery);
                }
            }

            ScrollTo();

            if (!IsLoading)
            {
                await Load().ConfigureAwait(false);
            }
        }

        internal static async void OpenFullscreenGallery()
        {
            if (Pics.Count < 1)
            {
                return;
            }

            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

            if (fakeWindow == null)
            {                
                fakeWindow = new FakeWindow();
            }

            if (mainWindow.bg.Children.Contains(picGallery))
            {
                mainWindow.bg.Children.Remove(picGallery);
                fakeWindow.grid.Children.Add(picGallery);
            }

            else if (!fakeWindow.grid.Children.Contains(picGallery))
            {
                mainWindow.bg.Children.Remove(picGallery);
                fakeWindow.grid.Children.Add(picGallery);
            }

            fakeWindow.Show();
            ScrollTo();
            mainWindow.Focus();

            if (!FreshStartup)
            {
                ScaleImage.TryFitImage();
            }            

            // Fix not showing up opacity bug.. 
            VisualStateManager.GoToElementState(picGallery, "Opacity", false);

            if (!IsLoading)
            {
                await Load().ConfigureAwait(false);
            }
        }

        #endregion

        #region Close

        internal static void CloseContainedGallery()
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

            var da = new DoubleAnimation {
                Duration = TimeSpan.FromSeconds(.5),
                From = 1,
                To = 0,
                FillBehavior = FillBehavior.Stop
            };
            da.Completed += delegate
            {
                picGallery.Visibility = Visibility.Collapsed;
                picGallery.Opacity = 1;
            };

            picGallery.BeginAnimation(UIElement.OpacityProperty, da);
        }

        internal static void CloseFullscreenGallery()
        {
            Properties.Settings.Default.PicGallery = 1;
            IsOpen = false;
            fakeWindow.Hide();

            Utilities.UpdateColor();

            HideInterfaceLogic.ShowStandardInterface();
        }

        #endregion

        #region Change

        internal static void ChangeToPicGalleryOne()
        {
            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            if (fakeWindow.grid.Children.Contains(picGallery))
            {
                fakeWindow.grid.Children.Remove(picGallery);
                mainWindow.bg.Children.Add(picGallery);
            }

            fakeWindow.Hide();
        }

        internal static void ChangeToPicGalleryTwo()
        {
            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

            if (fakeWindow != null)
            {
                if (!fakeWindow.grid.Children.Contains(picGallery))
                {
                    mainWindow.bg.Children.Remove(picGallery);
                    fakeWindow.grid.Children.Add(picGallery);
                }
            }
            else
            {
                mainWindow.bg.Children.Remove(picGallery);
                fakeWindow = new FakeWindow();
                fakeWindow.grid.Children.Add(picGallery);
            }

            fakeWindow.Show();
            ScrollTo();
            mainWindow.Focus();
        }

        #endregion

    }
}
