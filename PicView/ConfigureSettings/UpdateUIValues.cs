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
        internal static async Task ChangeSortingAsync(short sorting)
        {
            if (sorting > -1 && sorting < 7)
            {
                Properties.Settings.Default.SortPreference = sorting;
            }
            else
            {
                Properties.Settings.Default.SortPreference = 0;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                SetTitle.SetLoadingString();

                if (sorting == -1)
                {
                    var sortcm = MainContextMenu.Items[5] as MenuItem;

                    var desc = sortcm.Items[8] as MenuItem;
                    var descHeader = desc.Header as RadioButton;

                    var asc = sortcm.Items[9] as MenuItem;
                    var ascHeader = asc.Header as RadioButton;

                    if (Properties.Settings.Default.Ascending)
                    {
                        descHeader.IsChecked = false;
                        ascHeader.IsChecked = true;
                    }
                    else
                    {
                        descHeader.IsChecked = true;
                        ascHeader.IsChecked = false;
                    }
                }
                else
                {
                    try
                    {
                        var sortcm = MainContextMenu.Items[5] as MenuItem;

                        var sort0 = sortcm.Items[0] as MenuItem;
                        var sort0Header = sort0.Header as RadioButton;

                        var sort1 = sortcm.Items[1] as MenuItem;
                        var sort1Header = sort1.Header as RadioButton;

                        var sort2 = sortcm.Items[2] as MenuItem;
                        var sort2Header = sort2.Header as RadioButton;

                        var sort3 = sortcm.Items[3] as MenuItem;
                        var sort3Header = sort3.Header as RadioButton;

                        var sort4 = sortcm.Items[4] as MenuItem;
                        var sort4Header = sort4.Header as RadioButton;

                        var sort5 = sortcm.Items[5] as MenuItem;
                        var sort5Header = sort5.Header as RadioButton;

                        var sort6 = sortcm.Items[6] as MenuItem;
                        var sort6Header = sort6.Header as RadioButton;

                        switch (sorting)
                        {
                            default:
                            case 0:
                                sort0Header.IsChecked = true;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = false;
                                break;

                            case 1:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = true;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = false;
                                break;

                            case 2:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = true;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = false;
                                break;

                            case 3:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = true;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = false;
                                break;

                            case 4:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = true;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = false;
                                break;

                            case 5:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = true;
                                sort6Header.IsChecked = false;
                                break;

                            case 6:
                                sort0Header.IsChecked = false;
                                sort1Header.IsChecked = false;
                                sort2Header.IsChecked = false;
                                sort3Header.IsChecked = false;
                                sort4Header.IsChecked = false;
                                sort5Header.IsChecked = false;
                                sort6Header.IsChecked = true;
                                break;
                        }
                    }
                    catch (System.Exception)
                    {
                        // Supress task cancelled
                    }
                }
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
                ShowTooltipMessage(Application.Current.Resources["LoopingDisabled"]);
            }
            else
            {
                Properties.Settings.Default.Looping = true;
                loopcmHeader.IsChecked = true;
                ShowTooltipMessage(Application.Current.Resources["LoopingEnabled"]);
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