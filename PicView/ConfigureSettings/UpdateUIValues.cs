using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
                Properties.Settings.Default.SortPreference = sorting;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                SetTitle.SetLoadingString();
            });

            FileInfo fileInfo;
            var preloadValue = Preloader.Get(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]);
            if (preloadValue is not null && preloadValue.fileInfo is not null)
            {
                fileInfo = preloadValue.fileInfo;
            }
            else
            {
                fileInfo = new FileInfo(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]);
            }

            Preloader.Clear();

            bool sortGallery = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > 0)
                {
                    sortGallery = true;
                }
            });

            if (sortGallery)
            {
                await GalleryFunctions.SortGallery(fileInfo).ConfigureAwait(false);
            }
            else
            {
                await FileHandling.FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
            }

            await ChangeImage.LoadPic.LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
        }

        internal static void SetScrolling()
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen
                || GalleryFunctions.IsVerticalFullscreenOpen
                || GalleryFunctions.IsHorizontalOpen) { return; }

            var settingscm = MainContextMenu.Items[7] as MenuItem;
            var scrollcm = settingscm.Items[1] as MenuItem;
            var scrollcmHeader = scrollcm.Header as CheckBox;

            if (Properties.Settings.Default.ScrollEnabled)
            {
                _ = SetScrollBehaviour(false);
                scrollcmHeader.IsChecked = false;
                UC.GetQuickSettingsMenu.ToggleScroll.IsChecked = false;
            }
            else
            {
                _ = SetScrollBehaviour(true);
                scrollcmHeader.IsChecked = true;
                UC.GetQuickSettingsMenu.ToggleScroll.IsChecked = true;
            }
        }

        internal static void SetScrolling(bool value)
        {
            Properties.Settings.Default.ScrollEnabled = value;
            SetScrolling();
        }

        internal static void SetLooping()
        {
            var settingscm = MainContextMenu.Items[7] as MenuItem;
            var loopcm = settingscm.Items[0] as MenuItem;
            var loopcmHeader = loopcm.Header as CheckBox;

            if (Properties.Settings.Default.Looping)
            {
                Properties.Settings.Default.Looping = false;
                loopcmHeader.IsChecked = false;
                UC.GetQuickSettingsMenu.ToggleLooping.IsChecked = false;
                ShowTooltipMessage(Application.Current.Resources["LoopingDisabled"]);
            }
            else
            {
                Properties.Settings.Default.Looping = true;
                loopcmHeader.IsChecked = true;
                UC.GetQuickSettingsMenu.ToggleLooping.IsChecked = true;
                ShowTooltipMessage(Application.Current.Resources["LoopingEnabled"]);
            }
        }

        internal static void SetTopMost()
        {
            if (Properties.Settings.Default.Fullscreen)
            {
                return;
            }

            var settingscm = (MenuItem)MainContextMenu.Items[7];
            var TopmostMenu = (MenuItem)settingscm.Items[4];
            var TopmostHeader = (CheckBox)TopmostMenu.Header;

            if (Properties.Settings.Default.TopMost)
            {
                Properties.Settings.Default.TopMost = false;
                ConfigureWindows.GetMainWindow.Topmost = false;
                TopmostHeader.IsChecked = false;

                if (ConfigureWindows.GetSettingsWindow is not null)
                {
                    ConfigureWindows.GetSettingsWindow.TopmostRadio.IsChecked = false;
                }
            }
            else
            {
                Properties.Settings.Default.TopMost = true;
                ConfigureWindows.GetMainWindow.Topmost = true;
                TopmostHeader.IsChecked = true;

                if (ConfigureWindows.GetSettingsWindow is not null)
                {
                    ConfigureWindows.GetSettingsWindow.TopmostRadio.IsChecked = true;
                }
            }
        }

        internal static async Task SetAutoFitAsync(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen) { return; }
            await SetScalingBehaviourAsync(Properties.Settings.Default.AutoFitWindow = !Properties.Settings.Default.AutoFitWindow, Properties.Settings.Default.FillImage).ConfigureAwait(false);
        }

        internal static async Task SetAutoFillAsync(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen) { return; }
            await SetScalingBehaviourAsync(Properties.Settings.Default.AutoFitWindow, !Properties.Settings.Default.FillImage).ConfigureAwait(false);
        }

        internal static async Task SetScalingBehaviourAsync(bool autoFit, bool fill)
        {
            Properties.Settings.Default.FillImage = fill;
            Properties.Settings.Default.AutoFitWindow = autoFit;

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

            await ScaleImage.TryFitImageAsync().ConfigureAwait(false);
        }

        internal static void SetBorderColorEnabled(object sender, RoutedEventArgs e)
        {
            bool value = !Properties.Settings.Default.WindowBorderColorEnabled;
            Properties.Settings.Default.WindowBorderColorEnabled = value;
            ConfigColors.UpdateColor(!value);
        }
    }
}