using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Keybindings;

public static class MouseShortcuts
{
    public static async Task MainWindow_PointerPressed(PointerPressedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var prop = e.GetCurrentPoint(topLevel).Properties;

        if (prop.IsXButton1Pressed)
        {
            await FunctionsHelper.OpenLastFile().ConfigureAwait(false);
        }
        else if (prop.IsXButton2Pressed)
        {
            await FunctionsHelper.OpenLastFile().ConfigureAwait(false);
        }
    }
}
