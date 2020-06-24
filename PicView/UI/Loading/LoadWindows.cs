using PicView.UI.Windows;
using System.Diagnostics;
using System.Windows;
using static PicView.Library.Fields;

namespace PicView.UI.Loading
{
    internal static class LoadWindows
    {
        internal static SettingsWindow settingsWindow;
        internal static InfoWindow infoWindow;
        internal static EffectsWindow effects;
        internal static BatchWindow resizeAndOptimize;

        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void HelpWindow()
        {
            if (infoWindow == null)
            {
                infoWindow = new InfoWindow
                {
                    Owner = TheMainWindow
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
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow
                {
                    Owner = TheMainWindow
                };

                settingsWindow.Show();
            }
            else
            {
                if (settingsWindow.Visibility == Visibility.Visible)
                {
                    settingsWindow.Focus();
                }
                else
                {
                    settingsWindow.Show();
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
                effects = new EffectsWindow
                {
                    Owner = TheMainWindow
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
                resizeAndOptimize = new BatchWindow
                {
                    Owner = TheMainWindow
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