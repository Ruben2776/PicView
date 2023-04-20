using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Loading;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.ConfigureSettings
{
    // ReSharper disable once InconsistentNaming
    internal static class UpdateUIValues
    {
        // Todo Rewrite to use MVVM.. One day.

        internal static async Task ChangeSortingAsync(FileLists.SortFilesBy sortFilesBy, bool changeOrder = false)
        {
            if (changeOrder == false)
            {
                Settings.Default.SortPreference = (int) sortFilesBy;
            }

            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, SetTitle.SetLoadingString);

            var originFolder = Path.GetDirectoryName(Navigation.InitialPath);

            var preloadValue = Preloader.Get(Navigation.FolderIndex);
            var fileInfo = preloadValue?.FileInfo ?? new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            
            Preloader.Clear();
            var sortGallery = false;
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > 0)
                    sortGallery = true;
            });

            if (sortGallery)
            {
                try
                {
                    await GalleryFunctions.SortGallery(new FileInfo(Navigation.InitialPath)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ShowTooltipMessage(e);
                    GalleryFunctions.Clear();
                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
                }
            }
            else
            {
                Navigation.Pics = FileLists.FileList(new FileInfo(Navigation.InitialPath));
            }

            Navigation.FolderIndex = Navigation.Pics.IndexOf(fileInfo.FullName);
            await Preloader.AddAsync(Navigation.FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);
            await LoadPic.LoadPicAtIndexAsync(Navigation.FolderIndex, fileInfo).ConfigureAwait(false);
        }

        internal static void SetScrolling()
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen
                || GalleryFunctions.IsHorizontalOpen
                || Rotation.RotationAngle != 0)
                return;

            var settingCcm = MainContextMenu.Items[7] as MenuItem;
            var scrollCm = settingCcm.Items[1] as MenuItem;
            var scrollCmHeader = scrollCm.Header as CheckBox;

            SetScrollBehaviour(Settings.Default.ScrollEnabled);
            scrollCmHeader.IsChecked = Settings.Default.ScrollEnabled;
            UC.GetQuickSettingsMenu.ToggleScroll.IsChecked = Settings.Default.ScrollEnabled;
        }

        internal static void SetScrolling(bool value)
        {
            Settings.Default.ScrollEnabled = value;
            SetScrolling();
        }

        internal static void SetLooping()
        {
            var settingsCm = MainContextMenu.Items[7] as MenuItem;
            var loopCm = settingsCm.Items[0] as MenuItem;
            var loopCmHeader = loopCm.Header as CheckBox;

            Settings.Default.Looping = !Settings.Default.Looping;
            loopCmHeader.IsChecked = Settings.Default.Looping;
            UC.GetQuickSettingsMenu.ToggleLooping.IsChecked = Settings.Default.Looping;

            ShowTooltipMessage(Settings.Default.Looping ?
                Application.Current.Resources["LoopingEnabled"] : Application.Current.Resources["LoopingDisabled"],
                UC.UserControls_Open());
        }

        internal static void SetTopMost()
        {
            if (Settings.Default.Fullscreen)
            {
                return;
            }

            var settingCcm = (MenuItem)MainContextMenu.Items[7];
            var topMostMenu = (MenuItem)settingCcm.Items[4];
            var topMostHeader = (CheckBox)topMostMenu.Header;

            Settings.Default.TopMost = !Settings.Default.TopMost;
            GetMainWindow.Topmost = Settings.Default.TopMost;
            topMostHeader.IsChecked = Settings.Default.TopMost;

            if (GetSettingsWindow is not null)
            {
                GetSettingsWindow.TopmostRadio.IsChecked = Settings.Default.TopMost;
            }

            if (UC.GetQuickSettingsMenu is not null)
            {
                UC.GetQuickSettingsMenu.StayOnTop.IsChecked = Settings.Default.TopMost;
            }
        }

        internal static void SetAutoFit(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen) { return; }
            SetScalingBehaviour(Settings.Default.AutoFitWindow = !Settings.Default.AutoFitWindow, Settings.Default.FillImage);
        }

        internal static void SetAutoFill(object sender, RoutedEventArgs e)
        {
            SetScalingBehaviour(Settings.Default.AutoFitWindow, !Settings.Default.FillImage);
            var settingsCm = MainContextMenu.Items[7] as MenuItem;
            var fillCm = settingsCm.Items[5] as MenuItem;
            var fillCmHeader = fillCm.Header as CheckBox;
            fillCmHeader.IsChecked = Settings.Default.FillImage;
        }

        internal static void SetScalingBehaviour(bool autoFit, bool fill)
        {
            Settings.Default.FillImage = fill;
            Settings.Default.AutoFitWindow = autoFit;

            UC.GetQuickSettingsMenu.SetFit.IsChecked = autoFit;
            UC.GetQuickSettingsMenu.ToggleFill.IsChecked = fill;
            
            if (!GalleryFunctions.IsHorizontalFullscreenOpen)
                WindowSizing.SetWindowBehavior();

            ScaleImage.TryFitImage();
            Settings.Default.Save();
        }

        internal static void ToggleQuickResize()
        {
            UC.Close_UserControls();

            if (UC.GetQuickResize is null)
            {
                LoadControls.LoadQuickResize();
            }
            if (UC.GetQuickResize.Visibility == Visibility.Collapsed)
            {
                UC.GetQuickResize.Show();
            }
            else
            {
                UC.GetQuickResize.Hide();
            }
        }

        internal static void ToggleIncludeSubdirectories()
        {
            Settings.Default.IncludeSubDirectories = !Settings.Default.IncludeSubDirectories;

            GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (GetSettingsWindow is not null)
                {
                    GetSettingsWindow.SubDirRadio.IsChecked = Settings.Default.IncludeSubDirectories;
                }
                if (UC.GetQuickSettingsMenu is not null)
                {
                    UC.GetQuickSettingsMenu.SearchSubDir.IsChecked = Settings.Default.IncludeSubDirectories;
                }
            });
            Settings.Default.Save();

            if (ErrorHandling.CheckOutOfRange()) { return; }
            var preloadValue = Preloader.Get(Navigation.FolderIndex);
            if (preloadValue is null) { return; }
            Navigation.Pics = FileLists.FileList(preloadValue.FileInfo);

            GetMainWindow.Dispatcher.Invoke(() =>
            {
                SetTitle.SetTitleString(preloadValue.BitmapSource.PixelWidth, preloadValue.BitmapSource.PixelHeight,
                    Navigation.FolderIndex, preloadValue.FileInfo);
            });
        }

        internal static void ChangeFlipButton(bool isChecked)
        {
            if (GetMainWindow.MainImage.Source is null) return;

            var mainFlipButtonPath = GetMainWindow.FlipPath;
            var menuFlipButtonPath = UC.GetImageSettingsMenu.FlipButton.FlipPath;

            if (isChecked)
            {
                mainFlipButtonPath.Data = menuFlipButtonPath.Data =
                    Geometry.Parse("M448,192l-128,96v-64H128v128h248c4.4,0,8,3.6,8,8v48c0,4.4-3.6,8-8,8H72c-4.4,0-8-3.6-8-8V168c0-4.4,3.6-8,8-8h248V96 L448, 192z");
            }

            else
            {
                mainFlipButtonPath.Data = menuFlipButtonPath.Data =
                    Geometry.Parse("M192,96v64h248c4.4,0,8,3.6,8,8v240c0,4.4-3.6,8-8,8H136c-4.4,0-8-3.6-8-8v-48c0-4.4,3.6-8,8-8h248V224H192v64L64,192 L192, 96z");
            }
        }

        internal static void SetCtrlToZoom(bool value)
        {
            if (Properties.Settings.Default.CtrlZoom == value)
            {
                return;
            }
            Properties.Settings.Default.CtrlZoom = value;

            if (GetSettingsWindow is not null)
            {
                if (value)
                {
                    GetSettingsWindow.CtrlZoom.IsChecked = true;
                    GetSettingsWindow.ScrollZoom.IsChecked = false;
                }
                else
                {
                    GetSettingsWindow.CtrlZoom.IsChecked = false;
                    GetSettingsWindow.ScrollZoom.IsChecked = true;
                }
            }
            var settingscm = (MenuItem)MainContextMenu.Items[7];
            var ctrlZoomMenu = (MenuItem)settingscm.Items[6];
            var ctrlZoomHeader = (CheckBox)ctrlZoomMenu.Header;
            ctrlZoomHeader.IsChecked = Settings.Default.CtrlZoom;
        }
    }
}