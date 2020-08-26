using PicView.ChangeImage;
using PicView.UILogic.PicGallery;
using PicView.Views.UserControls;
using PicView.Views.UserControls.Misc;
using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace PicView.UILogic
{
    internal static class UC
    {
        internal static ImageSettings GetImageSettingsMenu { get; set; }
        internal static FileMenu GetFileMenu { get; set; }
        internal static QuickSettingsMenu GetQuickSettingsMenu { get; set; }
        internal static ToolsAndEffectsMenu GetToolsAndEffectsMenu { get; set; }
        internal static ToolTipMessage GetToolTipMessage { get; set; }
        internal static AutoScrollSign GetAutoScrollSign { get; set; }
        internal static ClickArrow GetClickArrowLeft { get; set; }
        internal static ClickArrow GetClickArrowRight { get; set; }
        internal static X2 Getx2 { get; set; }
        internal static Minus GetMinus { get; set; }
        internal static Restorebutton GetRestorebutton { get; set; }
        internal static Views.UserControls.PicGallery GetPicGallery { get; set; }
        internal static GalleryShortcut GetGalleryShortcut { get; set; }
        internal static CroppingTool GetCropppingTool { get; set; }
        internal static ColorPicker GetColorPicker { get; set; }

        private static bool imageSettingsMenuOpen;
        private static bool fileMenuOpen;
        private static bool quickSettingsMenuOpen;
        private static bool toolsAndEffectsMenuOpen;

        #region Toggle open close menus

        /// <summary>
        /// Toggles whether ImageSettingsMenu is open or not with a fade animation
        /// </summary>
        internal static bool ImageSettingsMenuOpen
        {
            get { return imageSettingsMenuOpen; }
            set
            {
                imageSettingsMenuOpen = value;
                GetImageSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { GetImageSettingsMenu.Visibility = Visibility.Hidden; };
                }
                else
                {
                    da.To = 1;
                }

                if (GetImageSettingsMenu != null)
                {
                    GetImageSettingsMenu.BeginAnimation(UIElement.OpacityProperty, da);
                }
            }
        }

        /// <summary>
        /// Toggles whether FileMenu is open or not with a fade animation
        /// </summary>
        internal static bool FileMenuOpen
        {
            get { return fileMenuOpen; }
            set
            {
                fileMenuOpen = value;
                GetFileMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { GetFileMenu.Visibility = Visibility.Hidden; };
                }
                else
                {
                    da.To = 1;
                }

                if (GetFileMenu != null)
                {
                    GetFileMenu.BeginAnimation(UIElement.OpacityProperty, da);
                }
            }
        }

        /// <summary>
        /// Toggles whether QuickSettingsMenu is open or not with a fade animation
        /// </summary>
        internal static bool QuickSettingsMenuOpen
        {
            get { return quickSettingsMenuOpen; }
            set
            {
                quickSettingsMenuOpen = value;
                GetQuickSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate
                    {
                        GetQuickSettingsMenu.Visibility = Visibility.Hidden;
                        GetQuickSettingsMenu.ZoomSliderParent.Visibility = Visibility.Collapsed;
                    };
                }
                else
                {
                    da.To = 1;
                }

                if (GetQuickSettingsMenu != null)
                {
                    if (ConfigureWindows.GetMainWindow.MainImage.Source != null)
                    {
                        GetQuickSettingsMenu.GoToPic.GoToPicBox.Text =
                            (Navigation.FolderIndex + 1)
                            .ToString(System.Globalization.CultureInfo.CurrentCulture);
                    }

                    GetQuickSettingsMenu.BeginAnimation(UIElement.OpacityProperty, da);
                }
            }
        }

        /// <summary>
        /// Toggles whether FunctionsMenu is open or not with a fade animation
        /// </summary>
        internal static bool ToolsAndEffectsMenuOpen
        {
            get { return toolsAndEffectsMenuOpen; }
            set
            {
                toolsAndEffectsMenuOpen = value;
                GetToolsAndEffectsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { GetToolsAndEffectsMenu.Visibility = Visibility.Hidden; };
                }
                else
                {
                    da.To = 1;
                }

                if (GetToolsAndEffectsMenu != null)
                {
                    GetToolsAndEffectsMenu.BeginAnimation(UIElement.OpacityProperty, da);
                }
            }
        }

        /// <summary>
        /// Check if any UserControls are open
        /// </summary>
        /// <returns></returns>
        internal static bool UserControls_Open()
        {
            if (ImageSettingsMenuOpen)
            {
                return true;
            }

            if (FileMenuOpen)
            {
                return true;
            }

            if (QuickSettingsMenuOpen)
            {
                return true;
            }

            if (ToolsAndEffectsMenuOpen)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        internal static void Close_UserControls()
        {
            if (ImageSettingsMenuOpen)
            {
                ImageSettingsMenuOpen = false;
            }

            if (FileMenuOpen)
            {
                FileMenuOpen = false;
            }

            if (QuickSettingsMenuOpen)
            {
                QuickSettingsMenuOpen = false;
            }

            if (ToolsAndEffectsMenuOpen)
            {
                ToolsAndEffectsMenuOpen = false;
            }
        }

        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        internal static void Close_UserControls(object sender, RoutedEventArgs e)
        {
            Close_UserControls();
        }

        /// <summary>
        /// Toggles whether open menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_open_menu(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen)
            {
                return;
            }

            FileMenuOpen = !FileMenuOpen;

            if (ImageSettingsMenuOpen)
            {
                ImageSettingsMenuOpen = false;
            }

            if (QuickSettingsMenuOpen)
            {
                QuickSettingsMenuOpen = false;
            }

            if (ToolsAndEffectsMenuOpen)
            {
                ToolsAndEffectsMenuOpen = false;
            }
        }

        /// <summary>
        /// Toggles whether image menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_image_menu(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen)
            {
                return;
            }

            ImageSettingsMenuOpen = !ImageSettingsMenuOpen;

            if (FileMenuOpen)
            {
                FileMenuOpen = false;
            }

            if (QuickSettingsMenuOpen)
            {
                QuickSettingsMenuOpen = false;
            }

            if (ToolsAndEffectsMenuOpen)
            {
                ToolsAndEffectsMenuOpen = false;
            }
        }

        /// <summary>
        /// Toggles whether quick settings menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_quick_settings_menu(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen)
            {
                return;
            }

            QuickSettingsMenuOpen = !QuickSettingsMenuOpen;

            if (FileMenuOpen)
            {
                FileMenuOpen = false;
            }

            if (ImageSettingsMenuOpen)
            {
                ImageSettingsMenuOpen = false;
            }

            if (ToolsAndEffectsMenuOpen)
            {
                ToolsAndEffectsMenuOpen = false;
            }
        }

        /// <summary>
        /// Toggles whether functions menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_Functions_menu(object sender, RoutedEventArgs e)
        {
            if (GalleryFunctions.IsOpen)
            {
                return;
            }

            ToolsAndEffectsMenuOpen = !ToolsAndEffectsMenuOpen;

            if (FileMenuOpen)
            {
                FileMenuOpen = false;
            }

            if (ImageSettingsMenuOpen)
            {
                ImageSettingsMenuOpen = false;
            }

            if (QuickSettingsMenuOpen)
            {
                QuickSettingsMenuOpen = false;
            }
        }

        #endregion Toggle open close menus
    }
}