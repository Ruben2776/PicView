using PicView.UI.Animations;
using PicView.UI.PicGallery;
using PicView.UI.UserControls.Misc;
using PicView.UI.Windows;
using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace PicView.UI.UserControls
{
    internal static class UC
    {
        internal static ImageSettings GetImageSettingsMenu;
        internal static FileMenu GetFileMenu;
        internal static QuickSettingsMenu GetQuickSettingsMenu;
        internal static ToolsAndEffectsMenu GetToolsAndEffectsMenu;
        internal static ToolTipMessage GetToolTipMessage;
        internal static AutoScrollSign GetAutoScrollSign;
        internal static ClickArrow GetClickArrowLeft;
        internal static ClickArrow GetClickArrowRight;
        internal static X2 Getx2;
        internal static Minus GetMinus;
        internal static PicGallery GetPicGallery;
        internal static GalleryShortcut GetGalleryShortcut;
        internal static CroppingTool GetCropppingTool;
        internal static ColorPicker GetColorPicker;

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
                    da.Completed += delegate { 
                        GetQuickSettingsMenu.Visibility = Visibility.Hidden;
                        GetQuickSettingsMenu.ZoomSliderParent.Visibility = Visibility.Collapsed; };
                }
                else
                {
                    da.To = 1;
                }

                if (GetQuickSettingsMenu != null)
                {
                    if (Library.Fields.TheMainWindow.MainImage.Source != null)
                    {
                        GetQuickSettingsMenu.GoToPicBox.Text =
                            (Library.Fields.FolderIndex + 1)
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