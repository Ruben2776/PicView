using Avalonia.Controls;
using PicView.Avalonia.Interfaces;

namespace PicView.Avalonia.Services;

/// <summary>
/// The secondary windows a main window can open. Each kind has at most one open window per main window.
/// </summary>
public enum SecondaryWindowKind
{
    About,
    ImageInfo,
    Keybindings,
    Settings,
    Effects,
    SingleImageResize,
    BatchResize,
    Convert,
    FileAssociations,
    PrintPreview
}

/// <summary>
/// Which window, if any, a secondary window is owned by when it is shown.
/// </summary>
public enum WindowOwner
{
    /// Shown without an owner on every platform.
    None,

    /// Owned by the main window on every platform.
    MainWindow,

    /// Owned by the main window everywhere except macOS, which shows it without an owner.
    MainWindowExceptMacOS
}

/// <summary>
/// Describes how to open one secondary window.
/// </summary>
/// <param name="Create">Creates the window through the platform's provider. Runs on the UI thread, and may return null.</param>
/// <param name="Owner">Which window owns the new window when it is shown.</param>
public sealed record SecondaryWindowSpec(Func<IWindowProvider, Window?> Create, WindowOwner Owner = WindowOwner.None)
{
    /// Runs on the UI thread after the window is created, and before it is shown.
    public Action<Window>? Configure { get; init; }

    /// Runs after the window is shown, and only when this open created it.
    public Func<Window, Task>? OnOpened { get; init; }

    /// Runs when the window closes, after its slot is cleared. Callers save their config and dispose objects here.
    public Func<Window, Task>? OnClosing { get; init; }
}
