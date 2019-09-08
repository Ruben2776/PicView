using System;
using System.Drawing;
using System.Windows;

namespace PicView
{
    /// <summary>
    /// Logic for the current monitor's screen resolution 
    /// </summary>
    internal class MonitorSize
    {
        internal int Width { get; set; }
        internal int Height { get; set; }

        internal double DpiScaling { get; set; }

        internal Rectangle WorkArea { get; set; }

        /// <summary>
        /// Store current monitor info
        /// </summary>
        /// <param name="width">The Pixel Width</param>
        /// <param name="height">The Pixel Height</param>
        /// <param name="dpiScaling"></param>
        /// <param name="workArea"></param>
        internal MonitorSize(int width, int height, double dpiScaling, Rectangle workArea)
        {
            Width = width;
            Height = height;
            DpiScaling = dpiScaling;
            WorkArea = workArea;
        }

        /// <summary>
        /// Get the current monitor's screen resolution
        /// </summary>
        /// <returns></returns>
        internal static MonitorSize GetMonitorSize()
        {
            /// TODO Get Solution to get actual screen pixzel size
            /// and not just without taskbar and such... 
            /// Maybe a solution not dependant on Windows Forms?

            // https://stackoverflow.com/a/32599760
            var currentMonitor = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle);

            //find out if our app is being scaled by the monitor
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            var dpiScaling = (source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1);

            //get the available area of the monitor
            var workArea = currentMonitor.WorkingArea;
            var workAreaWidth = (int)Math.Floor(workArea.Width * dpiScaling);
            var workAreaHeight = (int)Math.Floor(workArea.Height * dpiScaling);

            //return new MonitorSize(workAreaWidth, workAreaHeight, dpiScaling, workArea);
            return new MonitorSize(workAreaWidth, workAreaHeight, dpiScaling, workArea);
        }
    }




}
