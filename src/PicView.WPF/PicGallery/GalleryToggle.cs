using PicView.Core.Config;
using PicView.WPF.Animations;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.PicGallery.GalleryFunctions;
using static PicView.WPF.UILogic.ConfigureWindows;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.PicGallery
{
    internal static class GalleryToggle
    {
        #region Open

        internal static void OpenLayout()
        {
            if (GetPicGallery == null)
            {
                GetPicGallery = new Views.UserControls.Gallery.PicGallery
                {
                    Opacity = 0
                };
                Panel.SetZIndex(GetPicGallery, 2);
                GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
            }

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && IsGalleryOpen == false)
            {
                ShowBottomGallery();
                ScaleImage.TryFitImage();
            }
            else
            {
                // Make sure booleans are correct
                IsGalleryOpen = true;

                // Set size
                GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize);
                GetPicGallery.Width = GetMainWindow.ParentContainer.ActualWidth;
                GetPicGallery.Height = GetMainWindow.ParentContainer.ActualHeight;

                // Set alignment
                GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                GetPicGallery.VerticalAlignment = VerticalAlignment.Stretch;

                // Set scrollbar visibility and orientation
                GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                GetPicGallery.Container.Orientation = Orientation.Vertical;

                // Set style
                GetPicGallery.x2.Visibility = Visibility.Visible;
                GetPicGallery.Container.Margin = new Thickness(0, 60 * WindowSizing.MonitorInfo.DpiScaling, 0, 0);
                GetPicGallery.border.BorderThickness = new Thickness(1, 0, 0, 0);
                GetPicGallery.border.Background =
                    (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];
            }

            ReCalculateItemSizes();
        }

        internal static async Task OpenHorizontalGalleryAsync()
        {
            switch (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                case true when IsGalleryOpen:
                    await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                    {
                        if (GetPicGallery is null)
                        {
                            OpenLayout();
                        }

                        // Set size
                        GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize);
                        GetPicGallery.Width = GetMainWindow.ParentContainer.ActualWidth;

                        // Set alignment
                        GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;

                        // Set scrollbar visibility and orientation
                        GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                        GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        GetPicGallery.Container.Orientation = Orientation.Vertical;

                        // Set style
                        GetPicGallery.x2.Visibility = Visibility.Visible;
                        GetPicGallery.Container.Margin =
                            new Thickness(0, 60 * WindowSizing.MonitorInfo.DpiScaling, 0, 13);
                        GetPicGallery.border.BorderThickness = new Thickness(1, 0, 0, 0);
                        GetPicGallery.border.Background =
                            (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];
                        foreach (var child in GetPicGallery.Container.Children)
                        {
                            var item = (PicGalleryItem)child;
                            item.InnerBorder.Height = item.InnerBorder.Width = GalleryNavigation.PicGalleryItemSizeS;
                            item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
                        }

                        var heightAnimation = new DoubleAnimation
                        {
                            FillBehavior = FillBehavior.Stop,
                            AccelerationRatio = 0.7,
                            DecelerationRatio = 0.3,
                            From = GalleryNavigation.PicGalleryItemSize + 22,
                            To = GetMainWindow.ParentContainer.ActualHeight,
                            Duration = TimeSpan.FromSeconds(.5)
                        };

                        heightAnimation.Completed += delegate
                        {
                            GetPicGallery.Height = GetMainWindow.ParentContainer.ActualHeight;
                            GalleryNavigation.ScrollToGalleryCenter();
                        };

                        GetPicGallery.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
                    });
                    break;

                case true:
                    await GetMainWindow.Dispatcher.InvokeAsync(ShowBottomGallery, DispatcherPriority.Render);
                    ScaleImage.TryFitImage();
                    await GetPicGallery.Dispatcher.InvokeAsync(ReCalculateItemSizes, DispatcherPriority.Render);
                    break;

                default:
                    {
                        if (Pics?.Count < 1)
                        {
                            return;
                        }

                        IsGalleryOpen = true;

                        await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                        {
                            OpenLayout();
                            GetPicGallery.Visibility = Visibility.Visible;

                            var fade = AnimationHelper.Fade(GetPicGallery, TimeSpan.FromSeconds(.3), TimeSpan.Zero, 0, 1);
                            if (fade == false)
                            {
                                GetPicGallery.Opacity = 1;
                            }

                            GetClickArrowLeft.Visibility =
                                GetClickArrowRight.Visibility =
                                    GetX2.Visibility =
                                        GetMinus.Visibility =
                                            GetRestoreButton.Visibility =
                                                GetGalleryShortcut.Visibility = Visibility.Hidden;
                        });
                        break;
                    }
            }

            await LoadAndScrollToAsync().ConfigureAwait(false);
        }

        internal static void ShowBottomGallery()
        {
            GetPicGallery ??= new Views.UserControls.Gallery.PicGallery();
            Panel.SetZIndex(GetPicGallery, 2);
            GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
            GetPicGallery.Width = GetMainWindow.ParentContainer.ActualWidth;
            GetPicGallery.Height = GalleryNavigation.PicGalleryItemSize + 22;
            GetPicGallery.Visibility = Visibility.Visible;
            GetPicGallery.Opacity = 1;

            // Set alignment
            GetPicGallery.HorizontalAlignment = HorizontalAlignment.Center;
            GetPicGallery.VerticalAlignment = VerticalAlignment.Bottom;

            // Set scrollbar visibility and orientation
            GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            GetPicGallery.Container.Orientation = Orientation.Horizontal;

            // Set style
            GetPicGallery.x2.Visibility = Visibility.Collapsed;
            GetPicGallery.Container.Margin = new Thickness(0, 1, 0, 0);
            GetPicGallery.border.BorderThickness = new Thickness(1, 1, 1, 0);
            GetPicGallery.Container.MinHeight = GalleryNavigation.PicGalleryItemSize;
            GetPicGallery.border.Background =
                (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"];

            if (!GetMainWindow.ParentContainer.Children.Contains(GetPicGallery))
            {
                GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
            }

            GetStartUpUC?.ResponsiveSize(GetMainWindow.ActualWidth);
        }

        #endregion Open

        #region Close

        internal static void CloseCurrentGallery()
        {
            if (IsGalleryOpen)
            {
                CloseHorizontalGallery();
            }
        }

        internal static void CloseBottomGallery()
        {
            if (GetPicGallery is null)
            {
                return;
            }

            GetMainWindow.Dispatcher.Invoke(() =>
            {
                GetPicGallery.Visibility = Visibility.Collapsed;
                GetPicGallery.Opacity = 0;
                GetClickArrowLeft.Visibility =
                    GetClickArrowRight.Visibility =
                        GetX2.Visibility =
                            GetMinus.Visibility =
                                GetRestoreButton.Visibility =
                                    GetGalleryShortcut.Visibility = Visibility.Hidden;

                GetStartUpUC?.ResponsiveSize(GetMainWindow.ActualWidth);
                if (GetMainWindow.MainImage.Source is null) return;
                ScaleImage.FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
            });

            IsGalleryOpen = false;
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
        }

        internal static void CloseHorizontalGallery()
        {
            IsGalleryOpen = false;
            if (GetPicGallery is null)
            {
                return;
            }

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                var heightAnimation = new DoubleAnimation
                {
                    FillBehavior = FillBehavior.Stop,
                    AccelerationRatio = 0.5,
                    DecelerationRatio = 0.5,
                    From = GetPicGallery.ActualHeight,
                    To = GalleryNavigation.PicGalleryItemSize + 22,
                    Duration = TimeSpan.FromSeconds(.5)
                };
                GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
                for (var i = 0; i < GetPicGallery.Container.Children.Count; i++)
                {
                    var item = (PicGalleryItem)GetPicGallery.Container.Children[i];
                    item.InnerBorder.Height = item.InnerBorder.Width = GalleryNavigation.PicGalleryItemSize;
                    item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
                }

                heightAnimation.Completed += delegate
                {
                    ShowBottomGallery();
                    GalleryNavigation.ScrollToGalleryCenter();
                };

                GetPicGallery.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

                return;
            }

            // Restore interface elements if needed
            if (!SettingsHelper.Settings.UIProperties.ShowInterface || SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                HideInterfaceLogic.IsNavigationShown(true);
                HideInterfaceLogic.IsShortcutsShown(true);
            }

            var da = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.5),
                From = 1,
                To = 0,
                FillBehavior = FillBehavior.Stop
            };
            da.Completed += delegate { GetPicGallery.Visibility = Visibility.Collapsed; };

            GetPicGallery.BeginAnimation(UIElement.OpacityProperty, da);
        }

        #endregion Close

        #region Toggle

        internal static async Task ToggleGalleryAsync()
        {
            if (IsGalleryOpen)
            {
                CloseHorizontalGallery();
            }
            else if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                IsGalleryOpen = true; // Force open

                await OpenHorizontalGalleryAsync().ConfigureAwait(false);
            }
            else
            {
                await OpenHorizontalGalleryAsync().ConfigureAwait(false);
            }
        }

        #endregion Toggle

        private static async Task LoadAndScrollToAsync()
        {
            if (GalleryLoad.IsLoading == false)
            {
                var checkLoad = false;
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
                await GetMainWindow?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    GalleryNavigation.ScrollToGalleryCenter();
                    GalleryNavigation.SetSelected(FolderIndex, true);
                });
            }
            catch (TaskCanceledException)
            {
                // Suppress TaskCanceledException
            }
        }
    }
}