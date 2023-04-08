using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
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
    internal static class UpdateUIValues
    {
        // Todo Rewrite to use MVVM.. One day.

        internal static async Task ChangeSortingAsync(short sorting, bool changeOrder = false)
        {
            if (changeOrder == false)
            {
                Settings.Default.SortPreference = sorting;
            }

            await GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                SetTitle.SetLoadingString();
            });

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
                    Tooltip.ShowTooltipMessage(e);
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

            var settingscm = MainContextMenu.Items[7] as MenuItem;
            var scrollcm = settingscm.Items[1] as MenuItem;
            var scrollcmHeader = scrollcm.Header as CheckBox;

            if (Settings.Default.ScrollEnabled)
            {
                SetScrollBehaviour(false);
                scrollcmHeader.IsChecked = false;
                UC.GetQuickSettingsMenu.ToggleScroll.IsChecked = false;
            }
            else
            {
                SetScrollBehaviour(true);
                scrollcmHeader.IsChecked = true;
                UC.GetQuickSettingsMenu.ToggleScroll.IsChecked = true;
            }
        }

        internal static void SetScrolling(bool value)
        {
            Settings.Default.ScrollEnabled = value;
            SetScrolling();
        }

        internal static void SetLooping()
        {
            var settingscm = MainContextMenu.Items[7] as MenuItem;
            var loopcm = settingscm.Items[0] as MenuItem;
            var loopcmHeader = loopcm.Header as CheckBox;

            if (Settings.Default.Looping)
            {
                Settings.Default.Looping = false;
                loopcmHeader.IsChecked = false;
                UC.GetQuickSettingsMenu.ToggleLooping.IsChecked = false;
                ShowTooltipMessage(Application.Current.Resources["LoopingDisabled"]);
            }
            else
            {
                Settings.Default.Looping = true;
                loopcmHeader.IsChecked = true;
                UC.GetQuickSettingsMenu.ToggleLooping.IsChecked = true;
                ShowTooltipMessage(Application.Current.Resources["LoopingEnabled"]);
            }
        }

        internal static void SetTopMost()
        {
            if (Settings.Default.Fullscreen)
            {
                return;
            }

            var settingscm = (MenuItem)MainContextMenu.Items[7];
            var TopmostMenu = (MenuItem)settingscm.Items[4];
            var TopmostHeader = (CheckBox)TopmostMenu.Header;

            if (Settings.Default.TopMost)
            {
                Settings.Default.TopMost = false;
                GetMainWindow.Topmost = false;
                TopmostHeader.IsChecked = false;

                if (GetSettingsWindow is not null)
                {
                    GetSettingsWindow.TopmostRadio.IsChecked = false;
                }
            }
            else
            {
                Settings.Default.TopMost = true;
                GetMainWindow.Topmost = true;
                TopmostHeader.IsChecked = true;

                if (GetSettingsWindow is not null)
                {
                    GetSettingsWindow.TopmostRadio.IsChecked = true;
                }
            }
        }

        internal static void SetAutoFit(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen) { return; }
            SetScalingBehaviour(Settings.Default.AutoFitWindow = !Settings.Default.AutoFitWindow, Settings.Default.FillImage);
        }

        internal static void SetAutoFill(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen) { return; }
            SetScalingBehaviour(Settings.Default.AutoFitWindow, !Settings.Default.FillImage);
        }

        internal static void SetScalingBehaviour(bool autoFit, bool fill)
        {
            Settings.Default.FillImage = fill;
            Settings.Default.AutoFitWindow = autoFit;

            if (autoFit)
            {
                UC.GetQuickSettingsMenu.SetFit.IsChecked = true;
            }
            else
            {
                UC.GetQuickSettingsMenu.SetFit.IsChecked = false;
            }

            if (fill)
            {
                UC.GetQuickSettingsMenu.ToggleFill.IsChecked = true;
            }
            else
            {
                UC.GetQuickSettingsMenu.ToggleFill.IsChecked = false;
            }

            WindowSizing.SetWindowBehavior();

            ScaleImage.TryFitImage();
            Properties.Settings.Default.Save();
        }
    }
}