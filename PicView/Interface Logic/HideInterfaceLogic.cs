using System.Windows;
using System.Windows.Input;
using static PicView.FadeControls;
using static PicView.Fields;
using static PicView.Resize_and_Zoom;

namespace PicView
{
    internal static class HideInterfaceLogic
    {

        /// <summary>
        /// Toggle between hidden interface and default
        /// </summary>
        internal static void ToggleInterface()
        {
            if (Properties.Settings.Default.PicGallery == 2)
            {
                return;
            }

            // Hide interface
            if (Properties.Settings.Default.ShowInterface)
            {
                ShowTopandBottom(false);
                ShowNavigation(true);
                ShowShortcuts(true);

                Properties.Settings.Default.ShowInterface = false;

                if (activityTimer != null)
                {
                    activityTimer.Start();
                }
            }
            // Show interface
            else
            {
                Properties.Settings.Default.ShowInterface = true;

                ShowTopandBottom(true);
                ShowNavigation(false);
                ShowShortcuts(false);

                if (activityTimer != null)
                {
                    activityTimer.Stop();
                }
            }

            TryZoomFit();

            ToggleMenus.Close_UserControls();
        }

        internal static void ShowTopandBottom(bool show)
        {
            if (show)
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Toggle alternative layout navigation 
        /// </summary>
        /// <param name="show"></param>
        internal static void ShowNavigation(bool show)
        {
            if (clickArrowLeft == null && clickArrowRight == null && x2 == null && minus == null)
            {
                return;
            }

            if (show)
            {
                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility = Visibility.Visible;
            }
            else
            {
                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility = Visibility.Collapsed;
            }
        }

        internal static void ShowShortcuts(bool show)
        {
            if (galleryShortcut == null)
            {
                return;
            }

            if (show)
            {
                galleryShortcut.Visibility = Visibility.Visible;
            }
            else
            {
                galleryShortcut.Visibility = Visibility.Collapsed;
            }
        }



        /// <summary>
        /// Logic for mouse enter mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseEnter(object sender, MouseEventArgs e)
        {
            // Start timer when mouse enters
            activityTimer.Start();
            //FadeControlsAsync(true);
        }

        /// <summary>
        /// Logic for mouse enter mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseEnter_Negative(object sender, MouseEventArgs e)
        {
            // Start timer when mouse enters
            activityTimer.Stop();
            //FadeControlsAsync(true);
        }


        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Interface_MouseMove(object sender, MouseEventArgs e)
        {
            ////If Mouse is hidden, show it and interface elements.
            //if (e.MouseDevice.OverrideCursor == Cursors.None)
            //{
            //    Mouse.OverrideCursor = null;
            //    HideCursorTimer.Stop();
            //}


            // If mouse moves on mainwindow, show elements
            FadeControlsAsync(true);


            //// If Slideshow is running the interface will hide after 2,5 sec.
            //if (Slidetimer.Enabled == true)
            //{
            //    MouseIdleTimer.Start();
            //}
            //else
            //{
            //    MouseIdleTimer.Stop();
            //}
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
