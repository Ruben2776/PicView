using PicView.UI.PicGallery;
using PicView.UI.Sizing;
using PicView.UI.UserControls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileLists;
using static PicView.Library.Fields;
using static PicView.UI.Tooltip;
using static PicView.UI.TransformImage.Scroll;

namespace PicView.UI
{
    internal static class UpdateUIValues
    {
        internal static void ChangeSorting(short sorting)
        {
            if (Properties.Settings.Default.SortPreference == sorting)
            {
                return;
            }

            Properties.Settings.Default.SortPreference = sorting;
            var tmp = Pics[FolderIndex];
            if (!string.IsNullOrWhiteSpace(tmp))
            {
                Pics = FileList(Path.GetDirectoryName(tmp));
                PreloadCount = 4;
                Pic(Pics.IndexOf(tmp));
            }
            var sortcm = cm.Items[6] as MenuItem;

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

        internal static void SetScrolling(object sender, RoutedEventArgs e)
        {
            var settingscm = cm.Items[7] as MenuItem;
            var scrollcm = settingscm.Items[1] as MenuItem;
            var scrollcmHeader = scrollcm.Header as CheckBox;

            if (Properties.Settings.Default.ScrollEnabled)
            {
                IsScrollEnabled = false;
                scrollcmHeader.IsChecked = false;
                UC.quickSettingsMenu.ToggleScroll.IsChecked = false;
            }
            else
            {
                IsScrollEnabled = true;
                scrollcmHeader.IsChecked = true;
                UC.quickSettingsMenu.ToggleScroll.IsChecked = true;
            }
        }

        internal static void SetScrolling(bool value)
        {
            Properties.Settings.Default.ScrollEnabled = value;
            SetScrolling(null, null);
        }

        internal static void SetLooping(object sender, RoutedEventArgs e)
        {
            var settingscm = cm.Items[7] as MenuItem;
            var loopcm = settingscm.Items[0] as MenuItem;
            var loopcmHeader = loopcm.Header as CheckBox;

            if (Properties.Settings.Default.Looping)
            {
                Properties.Settings.Default.Looping = false;
                loopcmHeader.IsChecked = false;
                ShowTooltipMessage("Looping disabled");
            }
            else
            {
                Properties.Settings.Default.Looping = true;
                loopcmHeader.IsChecked = true;
                ShowTooltipMessage("Looping enabled");
            }
        }

        internal static void SetAutoFit(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen) { return; }
            SetScalingBehaviour(!Properties.Settings.Default.AutoFitWindow, Properties.Settings.Default.FillImage);
        }

        internal static void SetAutoFill(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen) { return; }
            SetScalingBehaviour(Properties.Settings.Default.AutoFitWindow, !Properties.Settings.Default.FillImage);
        }

        internal static void SetScalingBehaviour(bool windowBehaviour, bool fill)
        {
            if (windowBehaviour)
            {
                WindowLogic.SetWindowBehaviour = true;
                UC.quickSettingsMenu.SetFit.IsChecked = true;
            }
            else
            {
                WindowLogic.SetWindowBehaviour = false;
                UC.quickSettingsMenu.SetFit.IsChecked = false;
            }

            if (fill)
            {
                Properties.Settings.Default.FillImage = true;
                UC.quickSettingsMenu.ToggleFill.IsChecked = true;
            }
            else
            {
                Properties.Settings.Default.FillImage = false;
                UC.quickSettingsMenu.ToggleFill.IsChecked = false;
            }

            ScaleImage.TryFitImage();
        }

        internal static void SetBorderColorEnabled(object sender, RoutedEventArgs e)
        {
            bool value = Properties.Settings.Default.WindowBorderColorEnabled ? false : true;
            Properties.Settings.Default.WindowBorderColorEnabled = value;
            ConfigColors.UpdateColor(!value);
        }

        internal static void SetSlidetimer()
        {
            switch (Properties.Settings.Default.SlideTimer.ToString("0"))
            {
                case "1":
                    Properties.Settings.Default.SlideTimer = 1000;
                    break;

                case "2":
                    Properties.Settings.Default.SlideTimer = 2000;
                    break;

                case "3":
                    Properties.Settings.Default.SlideTimer = 3000;
                    break;

                case "4":
                    Properties.Settings.Default.SlideTimer = 4000;
                    break;

                case "5":
                    Properties.Settings.Default.SlideTimer = 5000;
                    break;

                case "6":
                    Properties.Settings.Default.SlideTimer = 6000;
                    break;

                case "7":
                    Properties.Settings.Default.SlideTimer = 7000;
                    break;

                case "8":
                    Properties.Settings.Default.SlideTimer = 8000;
                    break;

                case "9":
                    Properties.Settings.Default.SlideTimer = 9000;
                    break;

                case "10":
                    Properties.Settings.Default.SlideTimer = 10000;
                    break;

                case "11":
                    Properties.Settings.Default.SlideTimer = 11000;
                    break;

                case "12":
                    Properties.Settings.Default.SlideTimer = 12000;
                    break;

                case "13":
                    Properties.Settings.Default.SlideTimer = 13000;
                    break;

                case "14":
                    Properties.Settings.Default.SlideTimer = 140000;
                    break;

                case "15":
                    Properties.Settings.Default.SlideTimer = 15000;
                    break;

                default:
                    break;
            }
        }
    }
}