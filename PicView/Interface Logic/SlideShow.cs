using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.HideInterfaceLogic;
using static PicView.Navigation;
using static PicView.Tooltip;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class SlideShow
    {
        /// <summary>
        /// Maximize and removes Interface and start timer for slideshow.
        /// </summary>
        internal static void LoadSlideshow()
        {
            Slidetimer.Interval = Properties.Settings.Default.Slidetimeren;
            if (!File.Exists(Pics[FolderIndex]))
            {
                ToolTipStyle("There was no image(s) to show.");
                return;
            }

            if (mainWindow.WindowState == WindowState.Maximized)
                Maximize_Restore();

            ToggleInterface();
            mainWindow.Topmost = true;
            FitToWindow = false;
            mainWindow.Width = mainWindow.bg.Width = SystemParameters.PrimaryScreenWidth;
            mainWindow.Height = mainWindow.bg.Height = SystemParameters.PrimaryScreenHeight;
            mainWindow.Top = 0;
            mainWindow.Left = 0;

            Mouse.OverrideCursor = Cursors.None;
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED);
            SlideshowActive = true;
            Slidetimer.Start();
        }

        internal static void UnloadSlideshow()
        {
            ToggleInterface();
            mainWindow.Topmost = false;
            FitToWindow = true;
            mainWindow.bg.Width = double.NaN;
            mainWindow.bg.Height = double.NaN;

            Mouse.OverrideCursor = null;
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            SlideshowActive = false;
            Slidetimer.Stop();
        }

        /// <summary>
        /// Timer starts Slideshow Fade animation.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="e"></param>
        internal static async void SlideTimer_Elapsed(object server, System.Timers.ElapsedEventArgs e)
        {
            await Application.Current.MainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                AnimationHelper.Fade(mainWindow.img, 0, TimeSpan.FromSeconds(.5));
                Pic(true, false);
                AnimationHelper.Fade(mainWindow.img, 1, TimeSpan.FromSeconds(.5));
            }));
        }
    }
}
