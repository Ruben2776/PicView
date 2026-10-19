using Avalonia;
using Avalonia.Controls;
using System.Runtime.CompilerServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.UI;

public static class GenericWindowHelper
{
    private static readonly ConditionalWeakTable<Window, object> ClosedOnEscapePressWindows = new();

    /// <summary>
    /// Closes <paramref name="window"/> as Escape is pressed in it. The main window that opened it then ignores the
    /// press, which is still held when it takes focus.
    /// </summary>
    public static void CloseOnEscapePress(Window window)
    {
        ClosedOnEscapePressWindows.AddOrUpdate(window, ClosedOnEscapePressWindows);
        window.Close();
    }

    /// <summary>
    /// Whether <paramref name="window"/> was closed with <see cref="CloseOnEscapePress"/>.
    /// </summary>
    public static bool ClosedOnEscapePress(Window window) => ClosedOnEscapePressWindows.TryGetValue(window, out _);

    public static void AboutWindowInitialize(Window window)
    {
        if (Settings.UIProperties?.UserLanguage?.StartsWith("ja", StringComparison.OrdinalIgnoreCase) is true)
        {
            // Japanese already contains PicView in translation
            GenericWindowInitialize(window, TranslationManager.Translation.About);
        }
        else
        {
            GenericWindowInitialize(window, StringExtensions.CombineWithAppName(TranslationManager.Translation.About));
        }
    }
    
    public static void GenericWindowInitialize(Window window, string title, bool isWidthLocked, IWindowProperties windowConfig, bool closeOnEsc = true)
    {
        if (windowConfig.Width.HasValue && !double.IsFinite(windowConfig.Width.Value))
        {
            windowConfig.Width = null;
        }
        if (windowConfig.Height.HasValue && !double.IsFinite(windowConfig.Height.Value))
        {
            windowConfig.Height = null;
        }

        if (isWidthLocked)
        {
            if (!double.IsNaN(window.Width))
            {
                window.MinWidth = window.MaxWidth = window.Width;
            }
        }

        if (windowConfig.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
        else
        { 
            window.WindowState = WindowState.Normal;
            if (window.CanResize)
            {
                if (windowConfig.Height is { } h && double.IsFinite(h) && h > window.MinHeight)
                {
                    window.Height = h;
                }
                if (!isWidthLocked && windowConfig.Width is { } w && double.IsFinite(w) && w > window.MinWidth)
                {
                    window.Width = w;
                }
            }
            if (windowConfig is { Top: not null, Left: not null })
            {
                window.Position = new PixelPoint(windowConfig.Left.Value, windowConfig.Top.Value);
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                var screenSize = ScreenHelper.ScreenSize;
                var centerPixelPoint = new PixelPoint((int)(screenSize.Width / 2), (int)(screenSize.Height / 2));
                window.Position = centerPixelPoint;
            }
        }
            
        window.Title = StringExtensions.CombineWithAppName(title);

        if (closeOnEsc)
        {
            window.KeyUp += (_, e) =>
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
                e.Handled = true;
                window.Close();
            };
        }
        
        window.Closing += (_, _) =>
        {
            if (window.CanResize && window.WindowState is not WindowState.Maximized)
            {
                if (!isWidthLocked)
                {
                    var width = double.IsFinite(window.Bounds.Width) && window.Bounds.Width > 0
                        ? window.Bounds.Width
                        : window.Width;
                    if (double.IsFinite(width))
                    {
                        windowConfig.Width = width;
                    }
                }

                var height = double.IsFinite(window.Bounds.Height) && window.Bounds.Height > 0
                    ? window.Bounds.Height
                    : window.Height;
                if (double.IsFinite(height))
                {
                    windowConfig.Height = height;
                }
            }

            if (windowConfig.Width.HasValue && !double.IsFinite(windowConfig.Width.Value))
            {
                windowConfig.Width = null;
            }
            if (windowConfig.Height.HasValue && !double.IsFinite(windowConfig.Height.Value))
            {
                windowConfig.Height = null;
            }

            windowConfig.Maximized = window.WindowState is WindowState.Maximized;
            windowConfig.Top = window.Position.Y;
            windowConfig.Left = window.Position.X;

            if (window is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }
    
    public static void GenericWindowInitialize(Window window, string title)
    {
        window.Loaded += delegate
        {
            window.Title = title;
        };
        window.KeyDown += (_, e) =>
        {
            if (e.Key is not Key.Escape)
            {
                return;
            }

            e.Handled = true;
            CloseOnEscapePress(window);
        };
    }
}