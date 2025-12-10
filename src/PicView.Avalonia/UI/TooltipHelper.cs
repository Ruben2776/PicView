using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
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

    public static void StartTooltipSubscription(MainViewModel vm)
    {
        vm.ToolTip ??= new ToolTipViewModel();
        vm.ToolTip.ToolTipMessageSource
            .Where(msg => !string.IsNullOrWhiteSpace(msg)) // Ignore empty messages
            .Select(message => Observable.FromAsync(token => ShowToolTipAsync(
                message,
                vm.ToolTip.ToolTipMessageCentered.CurrentValue,
                vm.ToolTip.ToolTipMessageInterval.CurrentValue,
                token)))
            .Switch() // Switch to the latest message, cancelling the previous animation
            .Subscribe();
    }
    private static async ValueTask ShowToolTipAsync(string message, bool center, TimeSpan interval, CancellationToken cancellationToken)
    {
        // 1. Set the text and make the control visible
        UIHelper.GetToolTipMessage.ToolTipMessageText.Text = message;
        UIHelper.GetToolTipMessage.IsVisible = true;
        
        UIHelper.GetToolTipMessage.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
        UIHelper.GetToolTipMessage.VerticalAlignment =
            center ? VerticalAlignment.Center : VerticalAlignment.Bottom;

        // 2. Create and run the fade-in animation
        var fadeIn = AnimationsHelper.OpacityAnimation(0, 1, Speed);
        await fadeIn.RunAsync(UIHelper.GetToolTipMessage, cancellationToken);

        // Exit if a new message cancelled this task
        if (cancellationToken.IsCancellationRequested) return;

        // 3. Wait for a few seconds
        await Task.Delay(interval, cancellationToken);

        // Exit if a new message cancelled this task
        if (cancellationToken.IsCancellationRequested)
        {
            // If cancelled here, we still want to fade out smoothly
            var instantFadeOut = AnimationsHelper.OpacityAnimation(UIHelper.GetToolTipMessage.Opacity, 0, Speed);
            await instantFadeOut.RunAsync(UIHelper.GetToolTipMessage, cancellationToken);
            UIHelper.GetToolTipMessage.IsVisible = false;
            return;
        }

        // 4. Create and run the fade-out animation
        var fadeOut = AnimationsHelper.OpacityAnimation(1, 0, 0.3);
        await fadeOut.RunAsync(UIHelper.GetToolTipMessage, cancellationToken);
            
        // 5. Hide the control
        UIHelper.GetToolTipMessage.IsVisible = false;
    }
    
    public static async Task ShowTooltipMessageContinuallyAsync(object message, bool center, TimeSpan interval)
    {
        try
        {
            var endAnimation = AnimationsHelper.OpacityAnimation(1, 0, Speed);

            // ReSharper disable once MethodHasAsyncOverload
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var toolTip = UIHelper.GetToolTipMessage;
                toolTip.ToolTipMessageText.Text = message.ToString();
                UIHelper.GetToolTipMessage.IsVisible = true;
                
                if (!_isRunning)
                {
                    UIHelper.GetToolTipMessage.Margin = center ? new Thickness(0) : new Thickness(0, 0, 0, 15);
                    UIHelper.GetToolTipMessage.VerticalAlignment =
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
                    UIHelper.GetToolTipMessage.Opacity = 1;
                }, DispatcherPriority.Normal, _cancellationTokenSource.Token);
                await Task.Delay(interval, _cancellationTokenSource.Token);
                await endAnimation.RunAsync(UIHelper.GetToolTipMessage, _cancellationTokenSource.Token);
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
        var vm = Dispatcher.UIThread.Invoke(() => UIHelper.GetMainView.DataContext as MainViewModel);

        if (interval is not null)
        {
            vm.ToolTip.ToolTipMessageInterval.Value = interval.Value;
        }
        else
        {
            vm.ToolTip.ToolTipMessageInterval.Value = TimeSpan.FromSeconds(Speed);
        }
        vm.ToolTip.ToolTipMessageCentered.Value = center;
        vm.ToolTip.ToolTipMessageSource.Value = message.ToString();
    }
}