using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryToggle
    {
        #region Open

        internal static async Task OpenHorizontalGalleryAsync()
        {
            if (Pics?.Count < 1)
            {
                return;
            }

            IsHorizontalOpen = true;
            IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                GalleryLoad.LoadLayout();
                GetPicGallery.Visibility = Visibility.Visible;

                bool fade = AnimationHelper.Fade(GetPicGallery, TimeSpan.FromSeconds(.3), TimeSpan.Zero, 0, 1);
                if (fade == false)
                {
                    GetPicGallery.Opacity = 1;
                }

                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility =
                GetRestorebutton.Visibility =
                GetGalleryShortcut.Visibility = Visibility.Hidden;
            });

            await LoadAndScrollToAsync().ConfigureAwait(false);
        }

        internal static async Task OpenFullscreenGalleryAsync(bool startup)
        {
            if (Pics?.Count < 1 && !startup)
            {
                return;
            }

            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                GalleryLoad.LoadLayout();
                GetPicGallery.Visibility = Visibility.Visible;

                if (Settings.Default.FullscreenGalleryHorizontal)
                {
                    IsHorizontalFullscreenOpen = true;
                    IsHorizontalOpen = false;

                    var check = from x in GetMainWindow.ParentContainer.Children.OfType<PicGalleryTopButtons>()
                                select x;
                    foreach (var item in check)
                    {
                        GetMainWindow.ParentContainer.Children.Remove(item);
                    }

                    GetMainWindow.ParentContainer.Children.Add(new PicGalleryTopButtonsV2());
                }
                else
                {
                    IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

                    var check = from x in GetMainWindow.ParentContainer.Children.OfType<PicGalleryTopButtonsV2>()
                                select x;
                    foreach (var item in check)
                    {
                        GetMainWindow.ParentContainer.Children.Remove(item);
                    }

                    GetMainWindow.ParentContainer.Children.Add(new PicGalleryTopButtons());
                }

                GetMainWindow.Focus();

                // Fix not showing up opacity bug..
                VisualStateManager.GoToElementState(GetPicGallery, "Opacity", false);
                VisualStateManager.GoToElementState(GetPicGallery.Container, "Opacity", false);
                GetPicGallery.Opacity = GetPicGallery.Container.Opacity = 1;
            });

            if (startup == false)
            {
                ScaleImage.TryFitImage();
            }

            await LoadAndScrollToAsync().ConfigureAwait(false);
        }

        #endregion Open

        #region Close

        internal static void CloseCurrentGallery()
        {
            if (IsHorizontalFullscreenOpen)
            {
                CloseFullscreenGallery();
            }
            else if (IsHorizontalOpen)
            {
                CloseHorizontalGallery();
            }
        }

        internal static void CloseHorizontalGallery()
        {
            if (GetPicGallery is null) { return; }

            IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

            // Restore interface elements if needed
            if (!Settings.Default.ShowInterface || Settings.Default.Fullscreen)
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
            Properties.Settings.Default.FullscreenGalleryHorizontal = IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

            GetPicGallery.Visibility = Visibility.Collapsed;

            ConfigColors.UpdateColor();

            HideInterfaceLogic.ShowStandardInterface();
            GetPicGallery.x2.Visibility = Visibility.Collapsed;

            // Restore settings
            WindowSizing.SetWindowBehavior();

            if (Settings.Default.AutoFitWindow)
            {
                WindowSizing.CenterWindowOnScreen();
            }
            else
            {
                WindowSizing.SetLastWindowSize();
            }

            if (GetMainWindow.MainImage.Source is not null)
            {
                ScaleImage.FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
            }

            var check = from x in GetMainWindow.ParentContainer.Children.OfType<PicGalleryTopButtons>()
                        select x;
            if (check.Any())
            {
                GetMainWindow.ParentContainer.Children.Remove(check.ElementAt(0));
            }

            var check2 = from x in GetMainWindow.ParentContainer.Children.OfType<PicGalleryTopButtonsV2>()
                         select x;
            if (check2.Any())
            {
                GetMainWindow.ParentContainer.Children.Remove(check2.ElementAt(0));
            }

            GetMainWindow.Topmost = Properties.Settings.Default.TopMost;
        }

        #endregion Close

        private static async Task LoadAndScrollToAsync()
        {
            if (GalleryLoad.IsLoading == false)
            {
                bool checkLoad = false;
                await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    if (GetPicGallery.Container.Children.Count == Pics.Count)
                    {
                        checkLoad = true;
                    }
                });
                if (checkLoad == false)
                {
                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
                }
            }

            try
            {
                await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                {
                    GalleryNavigation.ScrollTo();
                });
            }
            catch (TaskCanceledException)
            {
                // Suppress TaskCanceledException
            }
        }
    }
}