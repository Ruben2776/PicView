using PicView.Properties;
using PicView.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace PicView.UILogic
{
    internal static class ConfigureWindows
    {
        internal static SettingsWindow? GetSettingsWindow { get; set; }
        internal static InfoWindow? GetInfoWindow { get; set; }
        internal static EffectsWindow? GetEffectsWindow { get; set; }
        internal static ImageInfoWindow? GetImageInfoWindow { get; set; }
        internal static ResizeWindow? GetResizeWindow { get; set; }

        /// <summary>
        /// The Main Window?
        /// </summary>
        internal static readonly MainWindow GetMainWindow = (MainWindow)Application.Current.MainWindow;

        /// <summary>
        /// Primary ContextMenu
        /// </summary>
        internal static ContextMenu? MainContextMenu { get; set; }

        internal static ContextMenu? WindowContextMenu { get; set; }

        internal static bool IsMainWindowTopMost
        {
            get { return Settings.Default.TopMost; }
            set
            {
                Settings.Default.TopMost = value;
                GetMainWindow.Topmost = value;
            }
        }

        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void InfoWindow()
        {
            if (GetInfoWindow == null)
            {
                GetInfoWindow = new InfoWindow { Owner = GetMainWindow };
                GetInfoWindow.Show();
            }
            else
            {
                if (GetInfoWindow.Visibility == Visibility.Visible)
                {
                    GetInfoWindow.Focus();
                }
                else
                {
                    GetInfoWindow.Show();
                }
            }
            if (Settings.Default.Fullscreen)
            {
                GetInfoWindow.Topmost = true;
                GetInfoWindow.BringIntoView();
            }
            else
            {
                GetInfoWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (GetSettingsWindow == null)
            {
                GetSettingsWindow = new SettingsWindow { Owner = GetMainWindow };

                GetSettingsWindow.Show();
            }
            else
            {
                if (GetSettingsWindow.Visibility == Visibility.Visible)
                {
                    GetSettingsWindow.Focus();
                }
                else
                {
                    GetSettingsWindow.Show();
                }
            }
            if (Settings.Default.Fullscreen)
            {
                GetSettingsWindow.Topmost = true;
                GetSettingsWindow.BringIntoView();
            }
            else
            {
                GetSettingsWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void EffectsWindow()
        {
            if (GetEffectsWindow == null)
            {
                GetEffectsWindow = new EffectsWindow { Owner = GetMainWindow };

                GetEffectsWindow.Show();
            }
            else
            {
                if (GetEffectsWindow.Visibility == Visibility.Visible)
                {
                    GetEffectsWindow.Focus();
                }
                else
                {
                    GetEffectsWindow.Show();
                }
            }
            if (Settings.Default.Fullscreen)
            {
                GetEffectsWindow.Topmost = true;
                GetEffectsWindow.BringIntoView();
            }
            else
            {
                GetEffectsWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show ImageInfo window
        /// </summary>
        internal static void ImageInfoWindow()
        {
            if (GetImageInfoWindow == null)
            {
                GetImageInfoWindow = new() { Owner = GetMainWindow };

                GetImageInfoWindow.Show();
            }
            else
            {
                if (GetImageInfoWindow.Visibility == Visibility.Visible)
                {
                    GetImageInfoWindow.Focus();
                }
                else
                {
                    GetImageInfoWindow.Show();
                }
            }
            if (Settings.Default.Fullscreen)
            {
                GetImageInfoWindow.Topmost = true;
                GetImageInfoWindow.BringIntoView();
            }
            else
            {
                GetImageInfoWindow.Topmost = false;
            }
        }

        internal static void ResizeWindow()
        {
            if (GetResizeWindow == null)
            {
                GetResizeWindow = new ResizeWindow { Owner = GetMainWindow };

                GetResizeWindow.Show();
            }
            else
            {
                if (GetResizeWindow.Visibility == Visibility.Visible)
                {
                    GetResizeWindow.Focus();
                }
                else
                {
                    GetResizeWindow.Show();
                }
            }
            if (Settings.Default.Fullscreen)
            {
                GetResizeWindow.Topmost = true;
                GetResizeWindow.BringIntoView();
            }
            else
            {
                GetResizeWindow.Topmost = false;
            }
        }

        #endregion Windows
    }
}