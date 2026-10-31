using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
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
    
    /// <summary>
    /// Displays a tooltip message with the specified configuration.
    /// </summary>
    /// <param name="vm">The Main Window to show the tooltip in.</param>
    /// <param name="message">The message content to be displayed in the tooltip.</param>
    /// <param name="center">Indicates whether the tooltip should be centered. Defaults to false.</param>
    /// <param name="interval">The time interval for which the tooltip is displayed. If null, a default interval is used.</param>
    public static void ShowTooltipMessage(MainWindowViewModel vm, object message, bool center = false,
        TimeSpan? interval = null)
    {
        var toolTip = vm.ToolTip;

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
    
    public static async Task ShowTooltipMessageAsync(object message, bool center, MainWindow mainWindow, TimeSpan interval)
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
                    Thickness tooltipMargin;
                    if (center)
                    {
                        tooltipMargin = new Thickness(0);
                    }
                    else
                    {
                        if (mainWindow.DataContext is MainWindowViewModel vm && vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsDockedGalleryVisible.CurrentValue)
                        {
                            var gallerySize = GalleryHelper.GetGallerySize(vm);
                            tooltipMargin = new Thickness(0, 0, 0,  gallerySize.height + 15);
                        }
                        else
                        {
                            tooltipMargin = new Thickness(0, 0, 0, 15);
                        }
                    }
                    mainWindow.UIHelper.GetToolTipMessage.Margin = tooltipMargin;
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
                await Task.Delay(interval, _cancellationTokenSource.Token).ConfigureAwait(true);
                await endAnimation.RunAsync(mainWindow.UIHelper.GetToolTipMessage, _cancellationTokenSource.Token).ConfigureAwait(true);
            }
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TooltipHelper), nameof(ShowTooltipMessageAsync), e);
        }
        finally
        {
            _isRunning = false;
        }
    }
}