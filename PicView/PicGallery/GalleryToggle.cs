using PicView.Animations;
using PicView.UILogic;
using PicView.Views.Windows;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryToggle
    {
        #region Toggle

        internal static async Task ToggleAsync(bool change = false)
        {
            if (Pics?.Count < 1)
            {
                return;
            }

            /// Toggle PicGallery, when not changed
            if (change == false)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal == false && Properties.Settings.Default.FullscreenGalleryVertical == false)
                {
                    if (IsHorizontalOpen == false)
                    {
                        await OpenHorizontalGalleryAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        CloseHorizontalGallery();
                    }
                }
                else
                {
                    if (IsHorizontalOpen == false)
                    {
                        await OpenFullscreenGalleryAsync(false).ConfigureAwait(false);
                    }
                    else
                    {
                        if (ConfigureWindows.GetFakeWindow is null || ConfigureWindows.GetFakeWindow is not null && ConfigureWindows.GetFakeWindow.IsVisible == false)
                        {
                            CloseHorizontalGallery();
                        }
                        else
                        {
                            CloseFullscreenGallery();
                        }
                    }
                }
            }
            /// Toggle PicGallery, when changed
            else
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal || Properties.Settings.Default.FullscreenGalleryVertical)
                {
                    ChangeToFullscreenGallery();
                }
                else
                {
                    ChangeToHorizontalGallery();
                }
            }
        }

        #endregion Toggle

        #region Open

        internal static async Task OpenHorizontalGalleryAsync()
        {
            if (Pics?.Count < 1)
            {
                return;
            }

            IsHorizontalOpen = true;
            IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                GalleryLoad.LoadLayout(false);
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

                if (GetFakeWindow != null)
                {
                    if (GetFakeWindow.grid.Children.Contains(GetPicGallery))
                    {
                        GetFakeWindow.grid.Children.Remove(GetPicGallery);
                        GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
                    }
                }
            });

            if (GetPicGallery.Container.Children.Count == 0)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }
            else if (GetPicGallery.Container.Children.Count == 1 && Pics.Count > 1)
            {
                GetPicGallery.Container.Children.Clear();
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            GalleryNavigation.SelectedGalleryItem = FolderIndex;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.ScrollTo();
            });
        }

        internal static async Task OpenFullscreenGalleryAsync(bool startup)
        {
            if (Pics?.Count < 1 && !startup)
            {
                return;
            }

            bool loadItems = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                GalleryLoad.LoadLayout(true);
                GetPicGallery.Visibility = Visibility.Visible;

                if (Properties.Settings.Default.FullscreenGalleryHorizontal)
                {
                    IsHorizontalFullscreenOpen = true;
                    IsVerticalFullscreenOpen = IsHorizontalOpen = false;
                }
                else
                {
                    IsVerticalFullscreenOpen = true;
                    IsHorizontalFullscreenOpen = IsHorizontalOpen = false;
                }

                if (GetFakeWindow == null)
                {
                    GetFakeWindow = new FakeWindow();

                    if (Properties.Settings.Default.FullscreenGalleryHorizontal)
                    {
                        GetFakeWindow.grid.Children.Add(new Views.UserControls.Gallery.PicGalleryTopButtonsV2
                        {
                            Margin = new Thickness(1, 12, 0, 0),
                        });
                    }
                    else
                    {
                        GetFakeWindow.grid.Children.Add(new Views.UserControls.Gallery.PicGalleryTopButtons
                        {
                            Margin = new Thickness(1, 12, 0, 0),
                        });
                    }
                }
                else
                {
                    GetFakeWindow.grid.Children.RemoveAt(0);
                    if (Properties.Settings.Default.FullscreenGalleryHorizontal)
                    {
                        GetFakeWindow.grid.Children.Add(new Views.UserControls.Gallery.PicGalleryTopButtonsV2
                        {
                            Margin = new Thickness(1, 12, 0, 0),
                        });
                    }
                    else
                    {
                        GetFakeWindow.grid.Children.Add(new Views.UserControls.Gallery.PicGalleryTopButtons
                        {
                            Margin = new Thickness(1, 12, 0, 0),
                        });
                    }
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

                // Fix not showing up opacity bug..
                VisualStateManager.GoToElementState(GetPicGallery, "Opacity", false);
                VisualStateManager.GoToElementState(GetPicGallery.Container, "Opacity", false);
                GetPicGallery.Opacity = GetPicGallery.Container.Opacity = 1;

                if (GetPicGallery.Container.Children.Count == 0)
                {
                    loadItems = true;
                }
            });

            if (startup == false)
            {
                await UILogic.Sizing.ScaleImage.TryFitImageAsync().ConfigureAwait(false);
            }

            if (loadItems)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }
        }

        #endregion Open

        #region Close

        internal static void CloseHorizontalGallery()
        {
            if (UC.GetPicGallery is null) { return; }

            IsVerticalFullscreenOpen = IsHorizontalFullscreenOpen = IsHorizontalOpen = false;

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
            if (ConfigureWindows.GetFakeWindow is null) { return; }
            IsVerticalFullscreenOpen = IsHorizontalFullscreenOpen = IsHorizontalOpen = false;
            GetFakeWindow.Hide();

            ConfigureSettings.ConfigColors.UpdateColor();

            HideInterfaceLogic.ShowStandardInterface();
            GetPicGallery.x2.Visibility = Visibility.Collapsed;

            
            // Restore settings
            if (Properties.Settings.Default.AutoFitWindow)
            {
                UILogic.Sizing.WindowSizing.CenterWindowOnScreen();
            }
            else
            {
                UILogic.Sizing.WindowSizing.SetLastWindowSize();
            }
            UILogic.Sizing.WindowSizing.SetWindowBehavior();
            UILogic.Sizing.ScaleImage.FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
        }

        #endregion Close

        #region Change

        internal static void ChangeToHorizontalGallery()
        {
            GalleryLoad.LoadLayout(false);

            if (GetFakeWindow.grid.Children.Contains(GetPicGallery))
            {
                GetFakeWindow.grid.Children.Remove(GetPicGallery);
                GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
            }

            GetFakeWindow.Hide();

            IsVerticalFullscreenOpen = IsHorizontalFullscreenOpen = false;
            IsHorizontalOpen = true;
        }

        internal static void ChangeToFullscreenGallery()
        {
            GalleryLoad.LoadLayout(true);

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