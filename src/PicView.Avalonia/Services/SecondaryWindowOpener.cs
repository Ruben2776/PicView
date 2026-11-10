using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;

namespace PicView.Avalonia.Services;

/// <summary>
/// Opens each kind of secondary window at most once per main window. Opening one that is already open brings it to
/// the front. It creates, tracks, titles, places, owns and shows the window, and closes it on Escape. When the window
/// closes it clears its slot, remembers and saves where the window was, and disposes it.
/// </summary>
/// <param name="mainWindow">
/// The main window focus returns to when one of these windows closes, and whose menus close when one opens.
/// </param>
/// <param name="provider">Creates the windows for this platform.</param>
/// <param name="ownerWindow">The window a secondary window is owned by, when its spec asks for one.</param>
public sealed class SecondaryWindowOpener(
    Func<MainWindow?> mainWindow,
    IWindowProvider provider,
    Func<Window?> ownerWindow)
{
    private readonly Dictionary<SecondaryWindowKind, Window> _open = [];
    private readonly List<Window> _openWindows = [];

    /// <summary>
    /// The secondary window this opener opened most recently that is still open.
    /// </summary>
    public Window? NewestOpenWindow => _openWindows.Count > 0 ? _openWindows[^1] : null;

    /// <summary>
    /// Closes every window this opener opened that is still open.
    /// </summary>
    public void CloseAll()
    {
        foreach (var window in _openWindows.ToArray())
        {
            window.Close();
        }
    }

    /// <summary>
    /// Closes the window of this kind, if this opener has one open.
    /// </summary>
    public void Close(SecondaryWindowKind kind)
    {
        if (_open.TryGetValue(kind, out var window))
        {
            window.Close();
        }
    }

    /// <summary>
    /// Whether a window of this kind is open.
    /// </summary>
    public bool IsOpen(SecondaryWindowKind kind) => _open.ContainsKey(kind);

    /// <summary>
    /// Opens a window of this kind, or brings the open one to the front.
    /// </summary>
    /// <returns>The window, or null when the provider did not create one.</returns>
    public async Task<Window?> OpenAsync(SecondaryWindowKind kind, SecondaryWindowSpec spec)
    {
        var (window, isNew) = await Dispatcher.UIThread.InvokeAsync(() => ShowOrFocus(kind, spec));
        if (isNew && window is not null && spec.OnOpened is not null)
        {
            await spec.OnOpened(window).ConfigureAwait(false);
        }

        return window;
    }

    private (Window? Window, bool IsNew) ShowOrFocus(SecondaryWindowKind kind, SecondaryWindowSpec spec)
    {
        if (_open.TryGetValue(kind, out var open))
        {
            Bring(open);
            CloseMenus();
            return (open, false);
        }

        var window = spec.Create(provider);
        if (window is null)
        {
            CloseMenus();
            return (null, false);
        }

        _open[kind] = window;
        var closedByEscape = false;
        Track(window, () => closedByEscape);
        spec.Configure?.Invoke(window);
        window.Title = spec.Title;
        Place(window, spec.Placement);
        if (spec.CloseOnEscape)
        {
            window.KeyDown += (_, e) =>
            {
                if (e.Key is not Key.Escape)
                {
                    return;
                }

                // A recording Keybind box records Escape instead
                if (FocusedInput.OwnsKeybind(window.FocusManager?.GetFocusedElement(), InputDispatch.CancelKeybind))
                {
                    return;
                }

                closedByEscape = true;
                e.Handled = true;
                window.Close();
            };
        }

        Show(window, spec.Owner);
        window.Closing += async (_, _) =>
        {
            if (_open.TryGetValue(kind, out var current) && current == window)
            {
                _open.Remove(kind);
            }

            var remembered = spec.Placement as SecondaryWindowPlacement.Remembered;
            if (remembered is not null)
            {
                Remember(window, remembered.Properties);
            }

            // Back on the UI thread each time, which disposes the window. A hook that throws still gets the window
            // saved and disposed, and its exception goes on after that.
            try
            {
                if (spec.OnClosing is not null)
                {
                    await spec.OnClosing(window).ConfigureAwait(true);
                }
            }
            finally
            {
                try
                {
                    if (remembered is not null)
                    {
                        await remembered.Save().ConfigureAwait(true);
                    }
                }
                finally
                {
                    (window as IDisposable)?.Dispose();
                }
            }
        };
        CloseMenus();
        return (window, true);
    }

    /// <summary>
    /// Keeps track of a secondary window, and gives the main window focus back when it closes.
    /// </summary>
    /// <param name="closedByEscape">Whether Escape closed the window, read as it closes.</param>
    private void Track(Window window, Func<bool> closedByEscape)
    {
        _openWindows.Add(window);
        window.Closed += (_, _) =>
        {
            _openWindows.Remove(window);
            if (mainWindow() is not { } focused)
            {
                return;
            }

            focused.Focus();
            // Escape is still held as this window takes focus, and would go on to close it too
            if (closedByEscape())
            {
                focused.KeybindRouter.IgnoreUntilReleased(Key.Escape);
            }
        };
    }

    /// <summary>
    /// Places a window that is about to show.
    /// </summary>
    private static void Place(Window window, SecondaryWindowPlacement placement)
    {
        switch (placement)
        {
            case SecondaryWindowPlacement.CenterScreen:
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                break;
            case SecondaryWindowPlacement.CenterOwner:
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                break;
            case SecondaryWindowPlacement.Remembered remembered:
                Restore(window, remembered.Properties);
                break;
        }
    }

    /// <summary>
    /// Opens a window where it was last closed, or in the center of the screen when no position is remembered or the
    /// remembered one is on none of the screens. A maximized window gets its normal size and position first, so it
    /// returns to them when it is restored.
    /// </summary>
    private static void Restore(Window window, IWindowProperties properties)
    {
        var placement = RememberedPlacement.Restore(properties, new WindowLimits(window.CanResize, window.MinWidth,
            window.MaxWidth, window.MinHeight, window.MaxHeight, window.Width, window.Height), WindowFunctions.ScreenAreas(window));

        if (placement.Size is { } size)
        {
            window.Width = size.Width;
            window.Height = size.Height;
        }

        if (placement.Position is { } position)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint(position.Left, position.Top);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (placement.Maximize)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Writes where a closing window is into its properties.
    /// </summary>
    private static void Remember(Window window, IWindowProperties properties)
    {
        var width = window.Bounds.Width > 0 ? window.Bounds.Width : window.Width;
        var height = window.Bounds.Height > 0 ? window.Bounds.Height : window.Height;
        RememberedPlacement.Remember(properties, new WindowBounds(width, height, window.Position.X,
            window.Position.Y, window.WindowState is WindowState.Maximized,
            window.WindowState is WindowState.Minimized), window.CanResize);
    }

    private void Show(Window window, WindowOwner owner)
    {
        var parent = owner switch
        {
            WindowOwner.MainWindow => ownerWindow(),
            WindowOwner.MainWindowExceptMacOS when !OperatingSystem.IsMacOS() => ownerWindow(),
            _ => null
        };

        if (parent is null)
        {
            window.Show();
        }
        else
        {
            window.Show(parent);
        }
    }

    private void CloseMenus()
    {
        if (mainWindow()?.ViewModel is { } vm)
        {
            UIHelper.CloseMenus(vm);
        }
    }

    private static void Bring(Window window)
    {
        if (window.WindowState is WindowState.Minimized)
        {
            WindowFunctions.ShowMinimizedWindow(window);
        }
        else
        {
            window.Activate();
        }
    }
}
