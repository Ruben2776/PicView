using System;
using System.Drawing;
using System.Windows;

namespace PicView.SystemIntegration
{
    /// <summary>
    /// Logic for the current monitor's screen resolution
    /// </summary>
    internal readonly struct MonitorSize : IEquatable<MonitorSize>
    {
        internal readonly double Width { get; }
        internal readonly double Height { get; }

        internal readonly double DpiScaling { get; }

        internal readonly Rect WorkArea { get; }

        public bool Equals(MonitorSize other)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override bool Equals(object obj) => obj != null && obj is MonitorSize size && Equals(size);
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

        public static bool operator ==(MonitorSize e1, MonitorSize e2)
        {
            return e1.Equals(e2);
        }

        public static bool operator !=(MonitorSize e1, MonitorSize e2)
        {
            return !(e1 == e2);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Store current monitor info
        /// </summary>
        /// <param name="width">The WorkArea Pixel Width</param>
        /// <param name="height">The WorkArea Pixel Height</param>
        /// <param name="dpiScaling"></param>
        /// <param name="workArea"></param>
        private MonitorSize(double width, double height, double dpiScaling, Rect workArea)
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
            // https://stackoverflow.com/a/32599760
            var currentMonitor = WpfScreenHelper.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle);

            //find out if the app is being scaled by the monitor
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            var dpiScaling = source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1;

            //get the available area of the monitor
            var workArea = currentMonitor.WorkingArea;
            var MonitorWidth = currentMonitor.Bounds.Width * dpiScaling;
            var MonitorHeight = currentMonitor.Bounds.Height * dpiScaling;

            // Update values for lower resolutions
            if (MonitorWidth < 1850 * dpiScaling)
            {
                Application.Current.Resources["LargeButtonHeight"] = 30 * dpiScaling;
                Application.Current.Resources["ButtonHeight"] = 22 * dpiScaling;
                Application.Current.Resources["StandardPadding"] = new Thickness(15, 8, 5, 8);
            }

            return new MonitorSize(MonitorWidth, MonitorHeight, dpiScaling, workArea);
        }
    }
}