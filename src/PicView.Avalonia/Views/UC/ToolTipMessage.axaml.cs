using System.Reactive.Linq;
using Avalonia.Controls;
using PicView.Avalonia.Helpers;
using ReactiveUI;

namespace PicView.Avalonia.Views.UC;

public partial class ToolTipMessage : UserControl
{
    private bool _isRunning;

    public ToolTipMessage()
    {
        InitializeComponent();

        // Subscribe to the ToolTipMessageText.Text changes
        this.WhenAnyValue(x => x.ToolTipMessageText.Text)
            .Throttle(TimeSpan.FromMilliseconds(100)) // Avoid rapid consecutive changes
            .Where(text => !string.IsNullOrEmpty(text))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(async _ =>
            {
                await DoAnimation();
            })
            .Subscribe();
    }

    private async Task DoAnimation()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;

        // Start opacity animation from 0 to 1
        var fadeInAnimation = AnimationsHelper.OpacityAnimation(from: 0, to: 1, 1.5);
        await fadeInAnimation.RunAsync(this);

        // Wait for the duration before fading out
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Start opacity animation from 1 to 0
        var fadeOutAnimation = AnimationsHelper.OpacityAnimation(from: 1, to: 0, 1.5);
        await fadeOutAnimation.RunAsync(this);

        _isRunning = false;
    }
}
