using PicView.Views.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using static PicView.Library.Fields;

namespace PicView.UILogic.Loading
{
    internal static class LoadWindows
    {
        internal static SettingsWindow GetSettingsWindow { get; set; }
        internal static InfoWindow InfoWindow { get; set; }
        internal static EffectsWindow GetEffectsWindow { get; set; }
        internal static BatchWindow GetResizeAndOptimize { get; set; }
        internal static ImageInfoWindow GetImageInfoWindow { get; set; }

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
                    Owner = TheMainWindow,
                    Opacity = 0
                };
            }
            else
            {
                if (InfoWindow.Visibility == Visibility.Visible)
                {
                    InfoWindow.Focus();
                }
            }

            InfoWindow.Width = TheMainWindow.ActualWidth;
            InfoWindow.Height = TheMainWindow.ActualHeight;

            InfoWindow.Left = TheMainWindow.Left + (TheMainWindow.Width - InfoWindow.Width) / 2;
            InfoWindow.Top = TheMainWindow.Top + (TheMainWindow.Height - InfoWindow.Height) / 2;

            TheMainWindow.Effect = new BlurEffect
            {
                RenderingBias = RenderingBias.Quality,
                KernelType = KernelType.Gaussian,
                Radius = 9
            };

            InfoWindow.BeginAnimation(Window.OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(.3)
            });

            InfoWindow.ShowDialog();
            
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
                    Owner = TheMainWindow
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
                    Owner = TheMainWindow
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
                    Owner = TheMainWindow
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
                    Owner = TheMainWindow
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