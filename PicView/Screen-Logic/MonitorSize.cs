using System;
using System.Drawing;
using System.Windows;

namespace PicView
{

    internal class MonitorSize
    {
        internal int Width { get; set; }
        internal int Height { get; set; }

        internal double DpiScaling { get; set; }

        internal Rectangle WorkArea { get; set; }

        internal MonitorSize(int width, int height, double dpiScaling, Rectangle workArea)
        {
            Width = width;
            Height = height;
            DpiScaling = dpiScaling;
            WorkArea = workArea;
        }

        internal static MonitorSize GetMonitorSize()
        {
            // https://stackoverflow.com/a/32599760
            var currentMonitor = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle);

            //find out if our app is being scaled by the monitor
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            var dpiScaling = (source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1);

            //get the available area of the monitor
            var workArea = currentMonitor.WorkingArea;
            var workAreaWidth = (int)Math.Floor(workArea.Width * dpiScaling);
            var workAreaHeight = (int)Math.Floor(workArea.Height * dpiScaling);

            return new MonitorSize(workAreaWidth, workAreaHeight, dpiScaling, workArea);
        }
    }




}
