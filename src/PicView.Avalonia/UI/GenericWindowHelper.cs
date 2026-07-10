using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.UI;

public static class GenericWindowHelper
{
    public static void AboutWindowInitialize(Window window)
    {
        if (Settings.UIProperties.UserLanguage.StartsWith("ja"))
        {
            // Japanese already contains PicView in translation
            GenericWindowInitialize(window, TranslationManager.Translation.About);
        }
        else
        {
            GenericWindowInitialize(window, StringExtensions.CombineWithAppName(TranslationManager.Translation.About));
        }
    }
    
    public static void GenericWindowInitialize(Window window, string title, bool isWidthLocked, IWindowProperties windowConfig)
    {
        window.Loaded += delegate
        {
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
                if (windowConfig.Height.HasValue && windowConfig.Height.Value > window.MinHeight)
                {
                    window.Height = windowConfig.Height.Value;
                }
                if (!isWidthLocked && windowConfig.Width.HasValue && windowConfig.Width.Value > window.MinWidth)
                {
                    window.Width = windowConfig.Width.Value;
                }
                if (windowConfig.Top is not null && windowConfig.Left is not null)
                {
                    window.Position = new PixelPoint(windowConfig.Left.Value, windowConfig.Top.Value);
                }
            }
            
            window.Title = StringExtensions.CombineWithAppName(title);
        };
        window.KeyUp += (_, e) =>
        {
            if (e.Key is not Key.Escape)
            {
                return;
            }

            if (!MainKeyboardShortcuts.IsEscKeyEnabled)
            {
                return;
            }
            e.Handled = true;
            MainKeyboardShortcuts.IsEscKeyEnabled = false;
            window.Close();
        };
        window.Closing += (_, _) =>
        {
            windowConfig.Width = window.Width;
            windowConfig.Height = window.Height;
            windowConfig.Maximized = window.WindowState is WindowState.Maximized;
            windowConfig.Top = window.Position.Y;
            windowConfig.Left = window.Position.X;
            MainKeyboardShortcuts.IsEscKeyEnabled = false;

            if (window is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.Focus();
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
            MainKeyboardShortcuts.IsEscKeyEnabled = false;
            window.Close();
        };
    }
}