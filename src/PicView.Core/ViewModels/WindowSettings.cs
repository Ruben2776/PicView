using PicView.Core.Config;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The settings one Main Window owns: seeded from the settings record when the window opens, and written back to it
/// only through <see cref="Store"/>.
/// </summary>
public sealed class WindowSettings : IDisposable
{
    private WindowSettings(bool isTopMost, bool isStayingCentered, RememberedWindowMode startingMode, NormalBounds bounds)
    {
        IsTopMost = new BindableReactiveProperty<bool>(isTopMost);
        IsStayingCentered = new BindableReactiveProperty<bool>(isStayingCentered);
        StartingMode = startingMode;
        Bounds = bounds;
    }

    public BindableReactiveProperty<bool> IsTopMost { get; }

    public BindableReactiveProperty<bool> IsStayingCentered { get; }

    /// <summary>
    /// The mode the window starts in. Never changes; the live mode is the window's WindowModeState.
    /// </summary>
    public RememberedWindowMode StartingMode { get; }

    /// <summary>
    /// The window's last size and position in Normal mode.
    /// </summary>
    public NormalBounds Bounds { get; set; }

    public static WindowSettings FromRecord(AppSettings? record)
    {
        var properties = record?.WindowProperties ?? new WindowProperties();
        return new WindowSettings(
            properties.TopMost,
            properties.KeepCentered,
            new RememberedWindowMode(properties.Fullscreen, properties.Maximized, properties.AutoFit),
            new NormalBounds(properties.Left, properties.Top, properties.Width, properties.Height));
    }

    /// <summary>
    /// The settings of a window opened from this one's window, whose remembered mode is <paramref name="openerMode"/>.
    /// </summary>
    public WindowSettings CopyFor(RememberedWindowMode openerMode) =>
        new(IsTopMost.Value, IsStayingCentered.Value, openerMode, Bounds);

    public void Store(AppSettings record, RememberedWindowMode remembered)
    {
        var properties = record.WindowProperties ??= new WindowProperties();
        properties.Fullscreen = remembered.Fullscreen;
        properties.Maximized = remembered.Maximized;
        properties.AutoFit = remembered.AutoFit;
        properties.TopMost = IsTopMost.Value;
        properties.KeepCentered = IsStayingCentered.Value;
        properties.Left = Bounds.Left;
        properties.Top = Bounds.Top;
        properties.Width = Bounds.Width;
        properties.Height = Bounds.Height;
    }

    public void Dispose()
    {
        IsTopMost.Dispose();
        IsStayingCentered.Dispose();
    }
}
