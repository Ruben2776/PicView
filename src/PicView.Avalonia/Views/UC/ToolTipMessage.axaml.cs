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
            .Throttle(TimeSpan.FromMilliseconds(500)) // Avoid rapid consecutive changes
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
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (Opacity < .2)
        {
            var fadeInAnimation = AnimationsHelper.OpacityAnimation(from: Opacity, to: 1, 1.5);
            await fadeInAnimation.RunAsync(this);
        }
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        if (!_isRunning)
        {
            var fadeOutAnimation = AnimationsHelper.OpacityAnimation(from: Opacity, to: 0, 1.5);
            await fadeOutAnimation.RunAsync(this);
        }
        _isRunning = false;
    }
}
