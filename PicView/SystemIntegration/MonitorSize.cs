﻿using System.Windows;
using System.Windows.Interop;
using WpfScreenHelper;

namespace PicView.SystemIntegration
{
    /// <summary>
    /// Represents information about the current monitor's screen resolution, DPI scaling and work area of a monitor screen.
    /// </summary>
    internal readonly struct MonitorSize : IEquatable<MonitorSize>
    {
        /// <summary>
        /// Gets the pixel width of the current monitor's working area.
        /// </summary>
        internal readonly double Width { get; }
        /// <summary>
        /// Gets the pixel height of the current monitor's working area.
        /// </summary>
        internal readonly double Height { get; }
        /// <summary>
        /// Gets the DPI scaling factor of the current monitor.
        /// </summary>
        internal readonly double DpiScaling { get; }
        /// <summary>
        /// Gets the available working area of the current monitor.
        /// </summary>
        internal readonly Rect WorkArea { get; }

        #region IEquatable<T>
        public bool Equals(MonitorSize other)
        {
            throw new NotImplementedException();
        }
        public override bool Equals(object? obj) => obj != null && obj is MonitorSize size && Equals(size);

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
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorSize"/> struct with the specified monitor information.
        /// </summary>
        /// <param name="width">The pixel width of the monitor's working area.</param>
        /// <param name="height">The pixel height of the monitor's working area.</param>
        /// <param name="dpiScaling">The DPI scaling factor of the current monitor.</param>
        /// <param name="workArea">The available working area of the current monitor.</param>
        private MonitorSize(double width, double height, double dpiScaling, Rect workArea)
        {
            Width = width;
            Height = height;
            DpiScaling = dpiScaling;
            WorkArea = workArea;
        }

        /// <summary>
        /// Gets the size and DPI scaling of the current monitor screen.
        /// </summary>
        /// <returns>A new instance of the <see cref="MonitorSize"/> struct representing the current monitor screen.</returns>
        internal static MonitorSize GetMonitorSize()
        {
            // Get the current monitor screen information
            var currentMonitor = Screen.FromHandle(new WindowInteropHelper(Application.Current.MainWindow).Handle);

            // Find out if the app is being scaled by the monitor
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            var dpiScaling = source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1;

            // Get the available work area of the monitor screen
            var workArea = currentMonitor.WorkingArea;
            var monitorWidth = currentMonitor.Bounds.Width * dpiScaling;
            var monitorHeight = currentMonitor.Bounds.Height * dpiScaling;

            // Return a new instance of the MonitorSize struct
            return new MonitorSize(monitorWidth, monitorHeight, dpiScaling, workArea);
        }
    }
}