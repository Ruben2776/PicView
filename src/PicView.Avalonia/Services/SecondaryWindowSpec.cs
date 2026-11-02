using Avalonia.Controls;
using PicView.Avalonia.Interfaces;
using PicView.Core.Config;

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
    /// The window's title, final: combined with the app name where the caller wants it.
    public required string Title { get; init; }

    /// Where the window opens, and whether its bounds are remembered.
    public required SecondaryWindowPlacement Placement { get; init; }

    /// Whether Escape closes the window. Keybindings turns it off, since it records keys.
    public bool CloseOnEscape { get; init; } = true;

    /// Runs on the UI thread after the window is created, and before it is shown.
    public Action<Window>? Configure { get; init; }

    /// Runs after the window is shown, and only when this open created it.
    public Func<Window, Task>? OnOpened { get; init; }

    /// Runs when the window closes, after its slot is cleared and its bounds are remembered, and before they are
    /// saved. Callers record their own state and dispose objects here.
    public Func<Window, Task>? OnClosing { get; init; }
}

/// <summary>
/// Where a Tool Window or App Window opens.
/// </summary>
public abstract record SecondaryWindowPlacement
{
    /// Opens where it was last closed, and remembers where it closes. Falls back to the center of the screen.
    public sealed record Remembered(IWindowProperties Properties, Func<Task> Save) : SecondaryWindowPlacement;

    /// Opens in the center of the screen.
    public sealed record CenterScreen : SecondaryWindowPlacement;

    /// Opens in the center of its owner, or of the screen when it has none.
    public sealed record CenterOwner : SecondaryWindowPlacement;
}
