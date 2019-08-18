using PicView.Native;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static PicView.Variables;
using static PicView.Interface;

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
            if (!File.Exists(PicPath))
            {
                ToolTipStyle("There was no image(s) to show.");
                return;
            }

            if (mainWindow.WindowState == WindowState.Maximized)
                mainWindow.Maximize_Restore();

            HideInterface(false);
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
            HideInterface();
            mainWindow.Topmost = false;
            FitToWindow = true;
            mainWindow.bg.Width = double.NaN;
            mainWindow.bg.Height = double.NaN;

            Mouse.OverrideCursor = null;
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            SlideshowActive = false;
            Slidetimer.Stop();
        }
    }
}
