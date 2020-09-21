using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using PicView.Views.Windows;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.PicGallery.GalleryLoad;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;
using PicView.PicGallery;

namespace PicView.UILogic.PicGallery
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
                        OpenHorizontalGallery();
                    }
                    else
                    {
                        CloseHorizontalGallery();
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

        #endregion Toggle

        #region Open

        internal static async void OpenHorizontalGallery()
        {
            if (Pics.Count < 1)
            {
                return;
            }

            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            AnimationHelper.Fade(GetPicGallery, TimeSpan.FromSeconds(.3), TimeSpan.Zero, 0, 1);

            GetClickArrowLeft.Visibility =
            GetClickArrowRight.Visibility =
            Getx2.Visibility =
            GetMinus.Visibility =
            GetRestorebutton.Visibility =
            GetGalleryShortcut.Visibility = Visibility.Hidden;

            if (GetFakeWindow != null)
            {
                if (GetFakeWindow.grid.Children.Contains(GetPicGallery))
                {
                    GetFakeWindow.grid.Children.Remove(GetPicGallery);
                    GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
                }
            }

            GalleryNavigation.SetSelected(FolderIndex);
            GalleryNavigation.ScrollTo();

            if (GetPicGallery.Container.Children.Count == 0)
            {
                await Load().ConfigureAwait(false);
            }
            // Fix weird bug when changing folder and one item remains
            // TODO investigate if selected gallery item does not get removed?
            else if (GetPicGallery.Container.Children.Count == 1 && Pics.Count > 1)
            {
                GetPicGallery.Container.Children.Clear();
                await Load().ConfigureAwait(false);
            }
        }

        internal static async void OpenFullscreenGallery(bool startup = false)
        {
            if (Pics.Count < 1 && !startup)
            {
                return;
            }

            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

            if (GetFakeWindow == null)
            {
                GetFakeWindow = new FakeWindow();
                GetFakeWindow.grid.Children.Add(new Views.UserControls.Gallery.PicGalleryTopButtons
                {
                    Margin = new Thickness(1, 12, 0, 0),
                });
            }

            // Switch gallery container to the correct window
            if (GetMainWindow.ParentContainer.Children.Contains(GetPicGallery))
            {
                GetMainWindow.ParentContainer.Children.Remove(GetPicGallery);
                GetFakeWindow.grid.Children.Add(GetPicGallery);
            }
            else if (!GetFakeWindow.grid.Children.Contains(GetPicGallery))
            {
                GetFakeWindow.grid.Children.Add(GetPicGallery);
            }

            GetFakeWindow.Show();
            GalleryNavigation.ScrollTo();
            GetMainWindow.Focus();

            if (!FreshStartup)
            {
                ScaleImage.TryFitImage();
            }

            // Fix not showing up opacity bug..
            VisualStateManager.GoToElementState(GetPicGallery, "Opacity", false);
            VisualStateManager.GoToElementState(GetPicGallery.Container, "Opacity", false);
            GetPicGallery.Opacity = GetPicGallery.Container.Opacity = 1;

            if (GetPicGallery.Container.Children.Count == 0)
            {
                await Load().ConfigureAwait(false);
            }
        }

        #endregion Open

        #region Close

        internal static void CloseHorizontalGallery()
        {
            IsOpen = false;

            // Restore interface elements if needed
            if (!Properties.Settings.Default.ShowInterface || Properties.Settings.Default.Fullscreen)
            {
                HideInterfaceLogic.ShowNavigation(true);
                HideInterfaceLogic.ShowShortcuts(true);
            }

            var da = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.5),
                From = 1,
                To = 0,
                FillBehavior = FillBehavior.Stop
            };
            da.Completed += delegate
            {
                GetPicGallery.Visibility = Visibility.Collapsed;
            };

            GetPicGallery.BeginAnimation(UIElement.OpacityProperty, da);
        }

        internal static void CloseFullscreenGallery()
        {
            Properties.Settings.Default.PicGallery = 1;
            IsOpen = false;
            GetFakeWindow.Hide();

            ConfigureSettings.ConfigColors.UpdateColor();

            HideInterfaceLogic.ShowStandardInterface();

            // Restore settings
            AutoFitWindow = Properties.Settings.Default.AutoFitWindow;
        }

        #endregion Close

        #region Change

        internal static void ChangeToPicGalleryOne()
        {
            Properties.Settings.Default.PicGallery = 1;
            LoadLayout();

            if (GetFakeWindow.grid.Children.Contains(GetPicGallery))
            {
                GetFakeWindow.grid.Children.Remove(GetPicGallery);
                GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
            }

            GetFakeWindow.Hide();
        }

        internal static void ChangeToPicGalleryTwo()
        {
            Properties.Settings.Default.PicGallery = 2;
            LoadLayout();

            if (GetFakeWindow != null)
            {
                if (!GetFakeWindow.grid.Children.Contains(GetPicGallery))
                {
                    GetMainWindow.ParentContainer.Children.Remove(GetPicGallery);
                    GetFakeWindow.grid.Children.Add(GetPicGallery);
                }
            }
            else
            {
                GetMainWindow.ParentContainer.Children.Remove(GetPicGallery);
                GetFakeWindow = new FakeWindow();
                GetFakeWindow.grid.Children.Add(GetPicGallery);
            }

            GetFakeWindow.Show();
            GalleryNavigation.ScrollTo();
            GetMainWindow.Focus();
        }

        #endregion Change
    }
}