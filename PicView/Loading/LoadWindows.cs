using PicView.UI.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
        internal static void InfoDialogWindow()
        {
            if (infoWindow == null)
            {
                infoWindow = new InfoWindow
                {
                    Owner = TheMainWindow,
                    Opacity = 0
                };
            }
            else
            {
                if (infoWindow.Visibility == Visibility.Visible)
                {
                    infoWindow.Focus();
                }
            }

            infoWindow.Width = TheMainWindow.ActualWidth;
            infoWindow.Height = TheMainWindow.ActualHeight;

            infoWindow.Left = TheMainWindow.Left + (TheMainWindow.Width - infoWindow.Width) / 2;
            infoWindow.Top = TheMainWindow.Top + (TheMainWindow.Height - infoWindow.Height) / 2;

            TheMainWindow.Effect = new BlurEffect
            {
                RenderingBias = RenderingBias.Quality,
                KernelType = KernelType.Gaussian,
                Radius = 9
            };

            infoWindow.BeginAnimation(Window.OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(.3)
            });

            infoWindow.ShowDialog();
            
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