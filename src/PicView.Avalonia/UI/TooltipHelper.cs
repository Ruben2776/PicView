using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace PicView.Avalonia.UI;

/// <summary>
/// Provides helper methods for displaying the tooltip in the application.
/// </summary>
public static class TooltipHelper
{
    private static bool _isRunning;

    /// <summary>
    /// Shows the tooltip message on the UI.
    /// </summary>
    /// <param name="message">The message to display in the tooltip.</param>
    /// <param name="center">Determines whether the tooltip should be centered or aligned at the bottom.</param>
    /// <param name="interval">The duration for which the tooltip should be visible.</param>
    public static void ShowTooltipMessage(object message, bool center, TimeSpan interval)
    {
        ToolTipMessage? toolTip = null;
        _isRunning = true;
        Dispatcher.UIThread.Invoke(() =>
        {
            toolTip = GetToolTipMessage;
            if (toolTip is null)
            {
                return;
            }
            toolTip.IsVisible = true;
            toolTip.ToolTipMessageText.Text = message.ToString();
            if (center)
            {
                toolTip.Margin = new Thickness(0, 0, 0, 0);
                toolTip.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                toolTip.Margin = new Thickness(0, 0, 0, 15);
                toolTip.VerticalAlignment = VerticalAlignment.Bottom;
            }
            toolTip.Opacity = 1;
        });
        var timer = new DispatcherTimer
        {
            Interval = interval
        };
        timer.Tick += (_, _) =>
        {
            if (!_isRunning)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    toolTip.Opacity = 0;
                });
            }
            _isRunning = false;
            timer.Stop();
        };
        timer.Start();
    }

    /// <summary>
    /// Shows the tooltip message on the UI with a default duration of 2 seconds.
    /// </summary>
    /// <param name="message">The message to display in the tooltip.</param>
    /// <param name="center">Determines whether the tooltip should be centered or aligned at the bottom.</param>
    internal static void ShowTooltipMessage(object message, bool center = false)
    {
        ShowTooltipMessage(message, center, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Gets the tooltip message control from the main view.
    /// </summary>
    private static ToolTipMessage? GetToolTipMessage
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return null;
            }
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            return mainView.MainGrid.Children.OfType<ToolTipMessage>().FirstOrDefault();
        }
    }

    /// <summary>
    /// Closes the tooltip message.
    /// </summary>
    internal static void CloseToolTipMessage()
    {
        if (GetToolTipMessage == null)
        {
            return;
        }

        if (GetToolTipMessage.CheckAccess())
        {
            GetToolTipMessage.IsVisible = false;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                GetToolTipMessage.IsVisible = false;
            });
        }
    }
}
