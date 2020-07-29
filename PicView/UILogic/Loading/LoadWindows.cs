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
        internal static InfoWindow InfoWindow { get; set; }
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
        internal static void InfoDialogWindow()
        {
            if (InfoWindow == null)
            {
                InfoWindow = new InfoWindow
                {
                    Owner = GetMainWindow
                };
            }
            else
            {
                if (InfoWindow.Visibility == Visibility.Visible)
                {
                    InfoWindow.Focus();
                }
                else
                {
                    InfoWindow.Visibility = Visibility.Visible;
                    InfoWindow.ShowDialog();
                }
            }


            InfoWindow.Show();

#if DEBUG
            Trace.WriteLine("HelpWindow loaded ");
#endif
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (GetSettingsWindow == null)
            {
                GetSettingsWindow = new SettingsWindow
                {
                    Owner = GetMainWindow
                };

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

#if DEBUG
            Trace.WriteLine("HelpWindow loaded ");
#endif
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void EffectsWindow()
        {
            if (GetEffectsWindow == null)
            {
                GetEffectsWindow = new EffectsWindow
                {
                    Owner = GetMainWindow
                };

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

#if DEBUG
            Trace.WriteLine("EffectsWindow loaded ");
#endif
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void ResizeAndOptimizeWindow()
        {
            if (GetResizeAndOptimize == null)
            {
                GetResizeAndOptimize = new BatchWindow
                {
                    Owner = GetMainWindow
                };

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

#if DEBUG
            Trace.WriteLine("EffectsWindow loaded ");
#endif
        }

        /// <summary>
        /// Show ImageInfo window
        /// </summary>
        internal static void ImageInfoWindow()
        {
            if (GetImageInfoWindow == null)
            {
                GetImageInfoWindow = new ImageInfoWindow
                {
                    Owner = GetMainWindow
                };

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

#if DEBUG
            Trace.WriteLine("GetImageInfoWindow loaded ");
#endif
        }

        #endregion Windows
    }
}