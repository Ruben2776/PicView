using PicView.UILogic.Loading;
using PicView.UILogic.PicGallery;
using System.Windows;
using System.Windows.Input;
using static PicView.UILogic.Animations.FadeControls;
using static PicView.UILogic.Sizing.ScaleImage;

namespace PicView.UILogic
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

            UC.Close_UserControls();
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
                LoadWindows.GetMainWindow.TitleBar.Visibility =
                LoadWindows.GetMainWindow.LowerBar.Visibility =
                LoadWindows.GetMainWindow.LeftBorderRectangle.Visibility =
                LoadWindows.GetMainWindow.RightBorderRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                LoadWindows.GetMainWindow.TitleBar.Visibility =
                LoadWindows.GetMainWindow.LowerBar.Visibility =
                LoadWindows.GetMainWindow.LeftBorderRectangle.Visibility =
                LoadWindows.GetMainWindow.RightBorderRectangle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Toggle alternative layout navigation
        /// </summary>
        /// <param name="show"></param>
        internal static void ShowNavigation(bool show)
        {
            if (UC.GetClickArrowLeft == null || UC.GetClickArrowRight == null
                || UC.Getx2 == null || UC.GetMinus == null || UC.GetRestorebutton == null)
            {
                return;
            }

            if (show)
            {
                UC.GetClickArrowLeft.Visibility =
                UC.GetClickArrowRight.Visibility =
                UC.Getx2.Visibility =
                UC.GetRestorebutton.Visibility =
                UC.GetMinus.Visibility = Visibility.Visible;
            }
            else
            {
                UC.GetClickArrowLeft.Visibility =
                UC.GetClickArrowRight.Visibility =
                UC.Getx2.Visibility =
                UC.GetRestorebutton.Visibility =
                UC.GetMinus.Visibility = Visibility.Collapsed;
            }
        }

        internal static void ShowShortcuts(bool show)
        {
            if (UC.GetGalleryShortcut == null)
            {
                return;
            }

            if (show)
            {
                UC.GetGalleryShortcut.Visibility = Visibility.Visible;
            }
            else
            {
                UC.GetGalleryShortcut.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseMove(object sender, MouseEventArgs e)
        {
            FadeControlsAsync(true);
        }

        /// <summary>
        /// Logic for mouse leave mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseLeave(object sender, MouseEventArgs e)
        {
            // Fade controis when mouse leaves mainwindow
            FadeControlsAsync(false);
        }
    }
}