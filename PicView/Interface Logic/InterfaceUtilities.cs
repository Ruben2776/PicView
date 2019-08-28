using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        internal static void HideInterface(bool slideshow = false, bool navigationButtons = true)
        {
            // Hide interface
            if (Properties.Settings.Default.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility
                = Visibility.Collapsed;

                if (navigationButtons)
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility = Visibility.Visible;
                else
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility = Visibility.Collapsed;

                if (!slideshow || !Properties.Settings.Default.Fullscreen)
                    Properties.Settings.Default.ShowInterface = false;

                if (activityTimer != null)
                    activityTimer.Start();
            }
            // Show interface
            else
            {
                Properties.Settings.Default.ShowInterface = true;

                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility = Visibility.Visible;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility = Visibility.Collapsed;

                if (activityTimer != null)
                    activityTimer.Stop();
            }
            if (xWidth != 0 && xHeight != 0)
                ZoomFit(xWidth, xHeight);

            ToggleMenus.Close_UserControls();
        }



        /// <summary>
        /// Logic for mouse enter mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            // Start timer when mouse enters mainwindow
            activityTimer.Start();
            //FadeControlsAsync(true);
        }


        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainWindow_MouseMove(object sender, MouseEventArgs e)
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
        internal static void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            // Start timer when mouse leaves mainwindow
            //activityTimer.Start();
            FadeControlsAsync(false);
        }      


    }
}
