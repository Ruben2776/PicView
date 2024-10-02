using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
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
    public static async Task ShowTooltipMessageAsync(object message, bool center, TimeSpan interval)
    {
        try
        {
            var toolTip = UIHelper.GetToolTipMessage;
            var startAnimation = AnimationsHelper.OpacityAnimation(0, 1, .6);
            var endAnimation = AnimationsHelper.OpacityAnimation(1, 0, .5);
        
            if (_isRunning)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UIHelper.GetToolTipMessage.Opacity = 1;
                    toolTip.ToolTipMessageText.Text = message.ToString();
                    toolTip.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
                    toolTip.VerticalAlignment = center ? VerticalAlignment.Center : VerticalAlignment.Bottom;
                });
                await Task.Delay(interval);
                await endAnimation.RunAsync(UIHelper.GetToolTipMessage);
                _isRunning = false;
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isRunning = true;
                UIHelper.GetToolTipMessage.IsVisible = true;
                UIHelper.GetToolTipMessage.ToolTipMessageText.Text = message.ToString();
                UIHelper.GetToolTipMessage.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
                UIHelper.GetToolTipMessage.VerticalAlignment = center ? VerticalAlignment.Center : VerticalAlignment.Bottom;
                UIHelper.GetToolTipMessage.Opacity = 0;
            });
            await startAnimation.RunAsync(UIHelper.GetToolTipMessage);
            await Task.Delay(interval);
            await endAnimation.RunAsync(UIHelper.GetToolTipMessage);
            _isRunning = false;
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
    }
    
    /// <summary>
    /// Shows the tooltip message on the UI.
    /// </summary>
    /// <param name="message">The message to display in the tooltip.</param>
    /// <param name="center">Determines whether the tooltip should be centered or aligned at the bottom.</param>
    /// <param name="interval">The duration for which the tooltip should be visible.</param>
    public static void ShowTooltipMessage(object message, bool center, TimeSpan interval)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (_, _) =>
        {
            if (!_isRunning)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var toolTip = UIHelper.GetToolTipMessage;
                    if (toolTip != null)
                    {
                        toolTip.Opacity = 0;
                    }
                });
            }
            _isRunning = false;
            timer.Stop();
        };
        timer.Start();
        
        try
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var toolTip = UIHelper.GetToolTipMessage;
                if (toolTip is null)
                {
                    return;
                }
                _isRunning = true;
                toolTip.IsVisible = true;
                toolTip.ToolTipMessageText.Text = message.ToString();
                toolTip.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
                toolTip.VerticalAlignment = center ? VerticalAlignment.Center : VerticalAlignment.Bottom;
                toolTip.Opacity = 1;
            });
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
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
    /// Shows the tooltip message on the UI with a default duration of 2 seconds.
    /// </summary>
    /// <param name="message">The message to display in the tooltip.</param>
    /// <param name="center">Determines whether the tooltip should be centered or aligned at the bottom.</param>
    internal static async Task ShowTooltipMessageAsync(object message, bool center = false)
    {
        await ShowTooltipMessageAsync(message, center, TimeSpan.FromSeconds(2));
    }
}
