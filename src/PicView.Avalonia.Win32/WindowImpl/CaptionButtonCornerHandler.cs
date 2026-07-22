using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using PicView.Core.WindowsNT;

namespace PicView.Avalonia.Win32.WindowImpl;

internal sealed class CaptionButtonCornerHandler
{
    private const double CaptionButtonSize = 30;
    private const uint WmCancelMode = 0x001F;
    private const uint WmNcHitTest = 0x0084;
    private const uint WmNcLeftButtonDown = 0x00A1;
    private const uint WmNcLeftButtonUp = 0x00A2;
    private const uint WmLeftButtonUp = 0x0202;
    private const uint WmCaptureChanged = 0x0215;
    private static readonly IntPtr HtClose = new(20);

    private readonly Window _window;
    private readonly Func<bool> _isEnabled;
    private bool _isCloseButtonPressed;

    public CaptionButtonCornerHandler(Window window, Func<bool> isEnabled)
    {
        _window = window;
        _isEnabled = isEnabled;
        _window.Opened += OnOpened;
        _window.Closed += OnClosed;
    }

    internal static void Attach(Window window, Func<bool>? isEnabled = null) =>
        _ = new CaptionButtonCornerHandler(window, isEnabled ?? (() => true));

    private void OnOpened(object? sender, EventArgs e) =>
        Win32Properties.AddWndProcHookCallback(_window, WndProc);

    private void OnClosed(object? sender, EventArgs e)
    {
        Win32Properties.RemoveWndProcHookCallback(_window, WndProc);
        _window.Opened -= OnOpened;
        _window.Closed -= OnClosed;
    }

    private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmCaptureChanged)
        {
            _isCloseButtonPressed = false;
            return IntPtr.Zero;
        }

        if (message == WmCancelMode)
        {
            CancelPress(hWnd);
            return IntPtr.Zero;
        }

        if (message == WmNcLeftButtonDown && wParam == HtClose && _isEnabled())
        {
            _isCloseButtonPressed = true;
            NativeMethods.SetCapture(hWnd);
            _isCloseButtonPressed = NativeMethods.GetCapture() == hWnd;
            handled = true;
            return IntPtr.Zero;
        }

        if ((message is WmLeftButtonUp or WmNcLeftButtonUp) && _isCloseButtonPressed)
        {
            var shouldClose = _isEnabled() && IsCursorInCloseButtonArea(hWnd);
            CancelPress(hWnd);
            handled = true;

            if (shouldClose)
            {
                Dispatcher.UIThread.Post(_window.Close);
            }

            return IntPtr.Zero;
        }

        if (message != WmNcHitTest || !_isEnabled() || !IsInCloseButtonArea(hWnd, lParam))
        {
            return IntPtr.Zero;
        }

        handled = true;
        return HtClose;
    }

    private void CancelPress(IntPtr hWnd)
    {
        _isCloseButtonPressed = false;
        if (NativeMethods.GetCapture() == hWnd)
        {
            NativeMethods.ReleaseCapture();
        }
    }

    private bool IsCursorInCloseButtonArea(IntPtr hWnd) =>
        NativeMethods.GetCursorPos(out var pointerPosition) &&
        IsInCloseButtonArea(hWnd, pointerPosition.X, pointerPosition.Y);

    private bool IsInCloseButtonArea(IntPtr hWnd, IntPtr lParam)
    {
        var packedPosition = lParam.ToInt64();
        var pointerX = (short)(packedPosition & 0xffff);
        var pointerY = (short)((packedPosition >> 16) & 0xffff);

        return IsInCloseButtonArea(hWnd, pointerX, pointerY);
    }

    private bool IsInCloseButtonArea(IntPtr hWnd, int pointerX, int pointerY)
    {
        if (!TryGetWindowArea(hWnd, out var windowArea))
        {
            return false;
        }

        var closeButtonSize = (int)Math.Ceiling(CaptionButtonSize * _window.RenderScaling);

        return pointerX >= windowArea.Right - closeButtonSize && pointerX < windowArea.Right &&
               pointerY >= windowArea.Y && pointerY < windowArea.Y + closeButtonSize;
    }

    private bool TryGetWindowArea(IntPtr hWnd, out PixelRect windowArea)
    {
        var screen = _window.Screens.ScreenFromWindow(_window);
        if (_window.WindowState == WindowState.FullScreen && screen is not null)
        {
            windowArea = screen.Bounds;
            return true;
        }

        if (_window.WindowState == WindowState.Maximized && screen is not null)
        {
            windowArea = screen.WorkingArea;
            return true;
        }

        if (NativeMethods.GetWindowRect(hWnd, out var windowRect))
        {
            windowArea = new PixelRect(
                windowRect.Left,
                windowRect.Top,
                windowRect.Right - windowRect.Left,
                windowRect.Bottom - windowRect.Top);
            return true;
        }

        windowArea = default;
        return false;
    }
}
