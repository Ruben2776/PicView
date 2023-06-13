﻿using PicView.UILogic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfScreenHelper;

namespace PicView.SystemIntegration;

/// <summary>
/// Represents information about the current monitor's screen resolution, DPI scaling and work area of a monitor screen.
/// </summary>
internal readonly struct MonitorSize : IEquatable<MonitorSize>
{
    /// <summary>
    /// Gets the pixel width of the current monitor's working area.
    /// </summary>
    internal double Width { get; }

    /// <summary>
    /// Gets the pixel height of the current monitor's working area.
    /// </summary>
    internal double Height { get; }

    /// <summary>
    /// Gets the DPI scaling factor of the current monitor.
    /// </summary>
    internal double DpiScaling { get; }

    /// <summary>
    /// Gets the available working area of the current monitor.
    /// </summary>
    internal Rect WorkArea { get; }

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

    #endregion IEquatable<T>

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
        if (Application.Current is null) // Fixes bug when closing window
            return new MonitorSize(SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height, 1,
                SystemParameters.WorkArea);

        Screen? currentMonitor = null;
        PresentationSource? source;
        double dpiScaling = 0;
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () => // Avoid threading errors
        {
            // Get the current monitor screen information
            currentMonitor = Screen.FromHandle(new WindowInteropHelper(Application.Current.MainWindow!).Handle);

            // Find out if the app is being scaled by the monitor
            source = PresentationSource.FromVisual(Application.Current.MainWindow!);
            dpiScaling = source is { CompositionTarget: not null } ? source.CompositionTarget.TransformFromDevice.M11 : 1;
        });

        // Get the available work area of the monitor screen
        Rect? workArea = currentMonitor.WorkingArea;
        var monitorWidth = currentMonitor.Bounds.Width * dpiScaling;
        var monitorHeight = currentMonitor.Bounds.Height * dpiScaling;

        // Return a new instance of the MonitorSize struct
        return new MonitorSize(monitorWidth, monitorHeight, dpiScaling, workArea.Value);
    }
}