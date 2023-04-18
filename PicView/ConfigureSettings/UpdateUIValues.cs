using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Loading;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.ConfigureSettings
{
    // ReSharper disable once InconsistentNaming
    internal static class UpdateUIValues
    {
        // Todo Rewrite to use MVVM.. One day.

        internal static async Task ChangeSortingAsync(short sorting, bool changeOrder = false)
        {
            if (changeOrder == false)
            {
                Settings.Default.SortPreference = sorting;
            }

            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, SetTitle.SetLoadingString);

            FileInfo fileInfo;
            var preloadValue = Preloader.Get(Navigation.FolderIndex);
            if (preloadValue is not null && preloadValue.FileInfo is not null)
            {
                fileInfo = preloadValue.FileInfo;
            }
            else
            {
                fileInfo = new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            }
            
            Preloader.Clear();
            bool sortGallery = false;
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > 0)
                    sortGallery = true;
            });

            if (sortGallery)
            {
                try
                {
                    await GalleryFunctions.SortGallery(fileInfo).ConfigureAwait(false);
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
                Navigation.Pics = FileLists.FileList(fileInfo);
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
    }
}