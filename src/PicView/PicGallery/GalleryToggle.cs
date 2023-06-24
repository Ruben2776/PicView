using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;
using Timer = System.Timers.Timer;

namespace PicView.PicGallery;

internal static class GalleryToggle
{
    #region Open

    internal static async Task OpenHorizontalGalleryAsync()
    {
        switch (Settings.Default.IsBottomGalleryShown)
        {
            case true when IsGalleryOpen:
                await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                {
                    // Set size
                    GalleryNavigation.SetSize(Settings.Default.ExpandedGalleryItems);
                    GetPicGallery.Width = GetMainWindow.ParentContainer.ActualWidth;

                    // Set alignment
                    GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;

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
                    foreach (var child in GetPicGallery.Container.Children)
                    {
                        var item = (PicGalleryItem)child;
                        item.InnerBorder.Height = item.InnerBorder.Width = GalleryNavigation.PicGalleryItemSize;
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
                await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, GalleryLoad.LoadBottomGallery);
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
                        GalleryLoad.LoadLayout();
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

    internal static async Task OpenFullscreenGalleryAsync()
    {
        await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            GalleryLoad.LoadLayout();
            GetPicGallery.Visibility = Visibility.Visible;

            if (Settings.Default.FullscreenGallery)
            {
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
                var check = from x in GetMainWindow.ParentContainer.Children.OfType<PicGalleryTopButtonsV2>()
                            select x;
                foreach (var item in check)
                {
                    GetMainWindow.ParentContainer.Children.Remove(item);
                }

                GetMainWindow.ParentContainer.Children.Add(new PicGalleryTopButtons());
            }

            GetMainWindow.Focus();

            // Fix not showing up opacity bug...
            VisualStateManager.GoToElementState(GetPicGallery, "Opacity", false);
            VisualStateManager.GoToElementState(GetPicGallery.Container, "Opacity", false);
            GetPicGallery.Opacity = GetPicGallery.Container.Opacity = 1;
        });

        ScaleImage.TryFitImage();
        await LoadAndScrollToAsync().ConfigureAwait(false);
    }

    #endregion Open

    #region Close

    internal static void CloseCurrentGallery()
    {
        if (Settings.Default.FullscreenGallery)
        {
            CloseFullscreenGallery();
        }
        else if (IsGalleryOpen)
        {
            CloseHorizontalGallery();
        }
    }

    internal static void CloseHorizontalGallery()
    {
        IsGalleryOpen = false;
        if (GetPicGallery is null) { return; }

        if (Settings.Default.IsBottomGalleryShown)
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
            GalleryNavigation.SetSize(Settings.Default.BottomGalleryItems);
            for (int i = 0; i < GetPicGallery.Container.Children.Count; i++)
            {
                var item = (PicGalleryItem)GetPicGallery.Container.Children[i];
                item.InnerBorder.Height = item.InnerBorder.Width = GalleryNavigation.PicGalleryItemSize;
                item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
            }
            heightAnimation.Completed += delegate
            {
                GalleryLoad.LoadBottomGallery();
                GalleryNavigation.ScrollToGalleryCenter();
            };

            GetPicGallery.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

            return;
        }

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
        Settings.Default.FullscreenGallery = IsGalleryOpen = false;

        GetPicGallery.Visibility = Visibility.Collapsed;

        ConfigColors.UpdateColor();

        HideInterfaceLogic.ToggleInterface();
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

        GetMainWindow.Topmost = Settings.Default.TopMost;
    }

    #endregion Close

    #region Toggle

    internal static async Task ToggleGalleryAsync()
    {
        if (IsGalleryOpen)
        {
            CloseHorizontalGallery();
        }
        else if (Settings.Default.FullscreenGallery == false)
        {
            if (Settings.Default.IsBottomGalleryShown)
            {
                IsGalleryOpen = true; // Force open
            }
            await OpenHorizontalGalleryAsync().ConfigureAwait(false);
        }
    }

    internal static async Task ToggleFullscreenGalleryAsync()
    {
        if (Settings.Default.FullscreenGallery)
        {
            CloseFullscreenGallery();
        }
        else
        {
            Settings.Default.FullscreenGallery = true;
            if (GetPicGallery is null) // Use timer to fix incorrect calculated size
            {
                var timer = new Timer(TimeSpan.FromSeconds(.4))
                {
                    AutoReset = false,
                    Enabled = true
                };
                timer.Elapsed += delegate
                {
                    GetMainWindow.Dispatcher.Invoke(() =>
                    {
                        if (GetMainWindow.MainImage.Source is null) return;
                        if (GetPicGallery == null)
                        {
                            GetPicGallery = new Views.UserControls.Gallery.PicGallery
                            {
                                Opacity = 0
                            };

                            GetMainWindow.ParentContainer.Children.Add(GetPicGallery);
                            Panel.SetZIndex(GetPicGallery, 999);
                        }
                        ScaleImage.FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                    });

                    timer.Dispose();
                };
            }
            await OpenFullscreenGalleryAsync().ConfigureAwait(false);
        }
    }

    #endregion Toggle

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
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }
        catch (TaskCanceledException)
        {
            // Suppress TaskCanceledException
        }
    }
}