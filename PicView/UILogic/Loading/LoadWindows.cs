using PicView.Views.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace PicView.UILogic.Loading
{
    internal static class LoadWindows
    {
        internal static SettingsWindow GetSettingsWindow { get; set; }
        internal static InfoWindow GetInfoWindow { get; set; }
        internal static EffectsWindow GetEffectsWindow { get; set; }
        internal static BatchWindow GetResizeAndOptimize { get; set; }
        internal static ImageInfoWindow GetImageInfoWindow { get; set; }

        /// <summary>
        /// The Main Window?
        /// </summary>
        internal static readonly MainWindow GetMainWindow = (MainWindow)Application.Current.MainWindow;

        /// <summary>
        /// Primary ContextMenu
        /// </summary>
        internal static System.Windows.Controls.ContextMenu cm;

        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void InfoWindow()
        {
            if (GetInfoWindow == null)
            {
                GetInfoWindow = new InfoWindow();
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
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (GetSettingsWindow == null)
            {
                GetSettingsWindow = new SettingsWindow();

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
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void EffectsWindow()
        {
            if (GetEffectsWindow == null)
            {
                GetEffectsWindow = new EffectsWindow();

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
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void ResizeAndOptimizeWindow()
        {
            if (GetResizeAndOptimize == null)
            {
                GetResizeAndOptimize = new BatchWindow();

                GetResizeAndOptimize.Show();
            }
            else
            {
                if (GetResizeAndOptimize.Visibility == Visibility.Visible)
                {
                    GetResizeAndOptimize.Focus();
                }
                else
                {
                    GetResizeAndOptimize.Show();
                }
            }
        }

        /// <summary>
        /// Show ImageInfo window
        /// </summary>
        internal static void ImageInfoWindow()
        {
            if (GetImageInfoWindow == null)
            {
                GetImageInfoWindow = new ImageInfoWindow();

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
        }

        #endregion Windows
    }
}