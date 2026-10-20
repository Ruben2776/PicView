using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Services;

/// <summary>
/// Opens each kind of secondary window at most once per main window. Opening one that is already open brings it to
/// the front. It creates, tracks, owns and shows the window, and clears its slot when the window closes.
/// </summary>
/// <param name="mainWindow">The main window focus returns to when one of these windows closes.</param>
/// <param name="provider">Creates the windows for this platform.</param>
/// <param name="ownerWindow">The window a secondary window is owned by, when its spec asks for one.</param>
/// <param name="core">Used to close the menus after an open. When it returns null, the menus are left alone.</param>
public sealed class SecondaryWindowOpener(
    Func<MainWindow?> mainWindow,
    IWindowProvider provider,
    Func<Window?> ownerWindow,
    Func<CoreViewModel?> core)
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
        Track(window);
        spec.Configure?.Invoke(window);
        Show(window, spec.Owner);
        window.Closing += async (_, _) =>
        {
            if (_open.TryGetValue(kind, out var current) && current == window)
            {
                _open.Remove(kind);
            }

            if (spec.OnClosing is not null)
            {
                await spec.OnClosing(window).ConfigureAwait(false);
            }
        };
        CloseMenus();
        return (window, true);
    }

    /// <summary>
    /// Keeps track of a secondary window, and gives the main window focus back when it closes.
    /// </summary>
    private void Track(Window window)
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
            if (GenericWindowHelper.ClosedOnEscapePress(window))
            {
                focused.KeybindRouter.IgnoreUntilReleased(Key.Escape);
            }
        };
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
        if (core() is { } vm)
        {
            UIHelper.CloseMenus(vm);
        }
    }

    private static void Bring(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            WindowFunctions.ShowMinimizedWindow(window);
        }
        else
        {
            window.Activate();
        }
    }
}
