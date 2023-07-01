using PicView.Animations;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

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
                    GalleryNavigation.SetSize(Settings.Default.ExpandedGalleryItemSize);
                    GetPicGallery.Width = GetMainWindow.ParentContainer.ActualWidth;

                    // Set alignment
                    GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;

                    // Set scrollbar visibility and orientation
                    GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    GetPicGallery.Container.Orientation = Orientation.Vertical;

                    // Set style
                    GetPicGallery.x2.Visibility = Visibility.Visible;
                    GetPicGallery.Container.Margin = new Thickness(0, 60 * WindowSizing.MonitorInfo.DpiScaling, 0, 13);
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
                await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, GalleryLoad.LoadBottomGallery);
                ScaleImage.TryFitImage();
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
        if (GetPicGallery is null) { return; }

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

            if (GetMainWindow.MainImage.Source is null) return;
            ScaleImage.FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
        });

        IsGalleryOpen = false;
        Settings.Default.IsBottomGalleryShown = false;
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
            GalleryNavigation.SetSize(Settings.Default.BottomGalleryItemSize);
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

    #endregion Close

    #region Toggle

    internal static async Task ToggleGalleryAsync()
    {
        if (IsGalleryOpen)
        {
            CloseHorizontalGallery();
        }
        else if (Settings.Default.IsBottomGalleryShown)
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
            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
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