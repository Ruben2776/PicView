using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Core.Config;

namespace PicView.Avalonia.Helpers;

public static class WindowHelper
{
    public static void InitializeWindowSizeAndPosition(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            desktop.MainWindow.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Top, (int)SettingsHelper.Settings.WindowProperties.Left);
            desktop.MainWindow.Width = SettingsHelper.Settings.WindowProperties.Width;
            desktop.MainWindow.Height = SettingsHelper.Settings.WindowProperties.Height;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                desktop.MainWindow.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Top, (int)SettingsHelper.Settings.WindowProperties.Left);
                desktop.MainWindow.Width = SettingsHelper.Settings.WindowProperties.Width;
                desktop.MainWindow.Height = SettingsHelper.Settings.WindowProperties.Height;
            }, DispatcherPriority.Normal).Wait();
        }

        _ = SettingsHelper.SaveSettingsAsync();
    }

    public static void UpdateWindowPosToSettings()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        SettingsHelper.Settings.WindowProperties.Top = desktop.MainWindow.Position.X;
        SettingsHelper.Settings.WindowProperties.Left = desktop.MainWindow.Position.Y;
        SettingsHelper.Settings.WindowProperties.Width = desktop.MainWindow.Width;
        SettingsHelper.Settings.WindowProperties.Height = desktop.MainWindow.Height;

        _ = SettingsHelper.SaveSettingsAsync();
    }

    public static void HandleWindowResize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (!SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }

        if (!size.OldValue.HasValue || !size.NewValue.HasValue)
        {
            return;
        }

        if (size.OldValue.Value.Width == 0 || size.OldValue.Value.Height == 0 ||
            size.NewValue.Value.Width == 0 || size.NewValue.Value.Height == 0)
        {
            return;
        }

        if (size.Sender != window)
        {
            return;
        }

        var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
        var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
    }
}