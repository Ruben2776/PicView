using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace PicView.Avalonia.UI;

/// <summary>
/// Provides helper methods for displaying the tooltip in the application.
/// </summary>
public static class TooltipHelper
{
    private const double Speed = 0.5;
    
    private static bool _isRunning;
    
    private static CancellationTokenSource? _cancellationTokenSource;

    public static void StartTooltipSubscription(ToolTipViewModel model, MainWindow mainWindow)
    {
        model.ToolTipMessageSource
            .Where(msg => !string.IsNullOrWhiteSpace(msg)) // Ignore empty messages
            .Select(message => Observable.FromAsync(token => ShowToolTipAsync(
                message,
                model.ToolTipMessageCentered.CurrentValue,
                mainWindow,
                model.ToolTipMessageInterval.CurrentValue,
                token)))
            .Switch() // Switch to the latest message, cancelling the previous animation
            .Subscribe();
    }
    private static async ValueTask ShowToolTipAsync(string message, bool center, MainWindow mainWindow, TimeSpan interval, CancellationToken cancellationToken)
    {
        mainWindow.UIHelper.GetToolTipMessage.ToolTipMessageText.Text = message;
        mainWindow.UIHelper.GetToolTipMessage.IsVisible = true;
        
        mainWindow.UIHelper.GetToolTipMessage.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
        mainWindow.UIHelper.GetToolTipMessage.VerticalAlignment =
            center ? VerticalAlignment.Center : VerticalAlignment.Bottom;

        // 2. Create and run the fade-in animation
        var fadeIn = AnimationsHelper.OpacityAnimation(0, 1, Speed);
        await fadeIn.RunAsync(mainWindow.UIHelper.GetToolTipMessage, cancellationToken);

        // Exit if a new message cancelled this task
        if (cancellationToken.IsCancellationRequested) return;

        // 3. Wait for a few seconds
        await Task.Delay(interval, cancellationToken);

        // Exit if a new message cancelled this task
        if (cancellationToken.IsCancellationRequested)
        {
            // If cancelled here, we still want to fade out smoothly
            var instantFadeOut = AnimationsHelper.OpacityAnimation(mainWindow.UIHelper.GetToolTipMessage.Opacity, 0, Speed);
            await instantFadeOut.RunAsync(mainWindow.UIHelper.GetToolTipMessage, cancellationToken);
            mainWindow.UIHelper.GetToolTipMessage.IsVisible = false;
            return;
        }

        // 4. Create and run the fade-out animation
        var fadeOut = AnimationsHelper.OpacityAnimation(1, 0, 0.3);
        await fadeOut.RunAsync(mainWindow.UIHelper.GetToolTipMessage, cancellationToken);
            
        // 5. Hide the control
        mainWindow.UIHelper.GetToolTipMessage.IsVisible = false;
    }
    
    public static async Task ShowTooltipMessageContinuallyAsync(object message, bool center, MainWindow mainWindow, TimeSpan interval)
    {
        try
        {
            var endAnimation = AnimationsHelper.OpacityAnimation(1, 0, Speed);

            // ReSharper disable once MethodHasAsyncOverload
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var toolTip = mainWindow.UIHelper.GetToolTipMessage;
                if (toolTip is null || message is null)
                {
                    return;
                }
                toolTip.ToolTipMessageText.Text = message.ToString();
                mainWindow.UIHelper.GetToolTipMessage.IsVisible = true;
                
                if (!_isRunning)
                {
                    mainWindow.UIHelper.GetToolTipMessage.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
                    mainWindow.UIHelper.GetToolTipMessage.VerticalAlignment =  
                        center ? VerticalAlignment.Center : VerticalAlignment.Bottom;
                }
                else
                {
                    toolTip.Opacity = 1;
                }
            }, DispatcherPriority.Normal, _cancellationTokenSource.Token);

            if (!_isRunning)
            {
                _isRunning = true;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.UIHelper.GetToolTipMessage.Opacity = 1;
                }, DispatcherPriority.Normal, _cancellationTokenSource.Token);
                await Task.Delay(interval, _cancellationTokenSource.Token);
                await endAnimation.RunAsync(mainWindow.UIHelper.GetToolTipMessage, _cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TooltipHelper), nameof(ShowTooltipMessageContinuallyAsync), e);
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Displays a tooltip message with the specified configuration.
    /// </summary>
    /// <param name="message">The message content to be displayed in the tooltip.</param>
    /// <param name="center">Indicates whether the tooltip should be centered. Defaults to false.</param>
    /// <param name="interval">The time interval for which the tooltip is displayed. If null, a default interval is used.</param>
    public static void ShowTooltipMessage(object message, bool center = false, TimeSpan? interval = null)
    {
        var core = Dispatcher.UIThread.Invoke(() => Application.Current.DataContext as CoreViewModel);
        if (core is null)
        {
            return;
        }

        var toolTip = core.MainWindows.ActiveWindow.CurrentValue.ToolTip;

        if (interval is not null)
        {
            toolTip.ToolTipMessageInterval.Value = interval.Value;
        }
        else
        {
            toolTip.ToolTipMessageInterval.Value = TimeSpan.FromSeconds(Speed);
        }
        toolTip.ToolTipMessageCentered.Value = center;
        toolTip.ToolTipMessageSource.Value = message.ToString();
    }
}