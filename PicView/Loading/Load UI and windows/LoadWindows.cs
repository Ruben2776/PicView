using PicView.Windows;
using System.Diagnostics;
using System.Windows;
using static PicView.Fields;

namespace PicView
{
    internal static class LoadWindows
    {
        internal static AllSettings allSettingsWindow;
        internal static Info infoWindow;
        internal static Effects effects;
        internal static ResizeAndOptimize resizeAndOptimize;

        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void HelpWindow()
        {
            if (infoWindow == null)
            {
                infoWindow = new Info
                {
                    Owner = mainWindow
                };

                infoWindow.Show();
            }
            else
            {
                if (infoWindow.Visibility == Visibility.Visible)
                {
                    infoWindow.Focus();
                }
                else
                {
                    infoWindow.Show();
                }
            }

#if DEBUG
            Trace.WriteLine("HelpWindow loaded ");
#endif
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (allSettingsWindow == null)
            {
                allSettingsWindow = new AllSettings
                {
                    Owner = mainWindow
                };

                allSettingsWindow.Show();
            }
            else
            {
                if (allSettingsWindow.Visibility == Visibility.Visible)
                {
                    allSettingsWindow.Focus();
                }
                else
                {
                    allSettingsWindow.Show();
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
            if (effects == null)
            {
                effects = new Effects
                {
                    Owner = mainWindow
                };

                effects.Show();
            }
            else
            {
                if (effects.Visibility == Visibility.Visible)
                {
                    effects.Focus();
                }
                else
                {
                    effects.Show();
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
            if (resizeAndOptimize == null)
            {
                resizeAndOptimize = new ResizeAndOptimize
                {
                    Owner = mainWindow
                };

                resizeAndOptimize.Show();
            }
            else
            {
                if (resizeAndOptimize.Visibility == Visibility.Visible)
                {
                    resizeAndOptimize.Focus();
                }
                else
                {
                    resizeAndOptimize.Show();
                }
            }

#if DEBUG
            Trace.WriteLine("EffectsWindow loaded ");
#endif
        }

        #endregion Windows
    }
}
