using PicView.UI.PicGallery;
using PicView.UI.TransformImage;
using System.Windows;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UI.Animations.FadeControls;
using static PicView.UI.Sizing.ScaleImage;
using static PicView.UI.UserControls.UC;

namespace PicView.UI
{
    internal static class HideInterfaceLogic
    {
        /// <summary>
        /// Toggle between hidden interface and default
        /// </summary>
        internal static void ToggleInterface()
        {
            if (Properties.Settings.Default.PicGallery == 2 && GalleryFunctions.IsOpen)
            {
                return;
            }

            // Hide interface
            if (Properties.Settings.Default.ShowInterface)
            {
                ShowMinimalInterface();
            }
            // Show interface
            else
            {
                ShowStandardInterface();
            }

            TryFitImage();

            Close_UserControls();
        }

        internal static void ShowStandardInterface()
        {
            Properties.Settings.Default.ShowInterface = true;

            ShowTopandBottom(true);
            ShowNavigation(false);
            ShowShortcuts(false);

            if (ActivityTimer != null)
            {
                ActivityTimer.Stop();
            }
        }

        internal static void ShowMinimalInterface()
        {
            ShowTopandBottom(false);
            ShowNavigation(true);
            ShowShortcuts(true);

            Properties.Settings.Default.ShowInterface = false;

            if (ActivityTimer != null)
            {
                ActivityTimer.Start();
            }
        }

        internal static void ShowTopandBottom(bool show)
        {
            if (show)
            {
                TheMainWindow.TitleBar.Visibility =
                TheMainWindow.LowerBar.Visibility =
                TheMainWindow.LeftBorderRectangle.Visibility =
                TheMainWindow.RightBorderRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                TheMainWindow.TitleBar.Visibility =
                TheMainWindow.LowerBar.Visibility =
                TheMainWindow.LeftBorderRectangle.Visibility =
                TheMainWindow.RightBorderRectangle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Toggle alternative layout navigation
        /// </summary>
        /// <param name="show"></param>
        internal static void ShowNavigation(bool show)
        {
            if (GetClickArrowLeft == null && GetClickArrowRight == null && Getx2 == null && GetMinus == null)
            {
                return;
            }

            if (show)
            {
                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility = Visibility.Visible;
            }
            else
            {
                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility = Visibility.Collapsed;
            }
        }

        internal static void ShowShortcuts(bool show)
        {
            if (GetGalleryShortcut == null)
            {
                return;
            }

            if (show)
            {
                GetGalleryShortcut.Visibility = Visibility.Visible;
            }
            else
            {
                GetGalleryShortcut.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Logic for mouse enter mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!ActivityTimer.Enabled || Scroll.IsAutoScrolling)
            {
                ActivityTimer.Start();
            }
        }

        /// <summary>
        /// Logic for mouse enter mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseEnter_Negative(object sender, MouseEventArgs e)
        {
            if (ActivityTimer.Enabled)
            {
                ActivityTimer.Stop();
            }
        }

        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseMove(object sender, MouseEventArgs e)
        {
            if (Scroll.IsAutoScrolling)
            {
                return;
            }

            if (GetCropppingTool != null)
            {
                if (GetCropppingTool.IsVisible)
                {
                    return;
                }
            }

            FadeControlsAsync(true);
        }

        /// <summary>
        /// Logic for mouse leave mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseLeave(object sender, MouseEventArgs e)
        {
            // Start timer when mouse leaves mainwindow
            //activityTimer.Start();
            FadeControlsAsync(false);
        }
    }
}