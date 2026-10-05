using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.Animations;
using PicView.Core.DebugTools;
using R3;

namespace PicView.Avalonia.CustomControls;

public class AnimatedMenu : UserControl, IDisposable
{
    public static readonly AvaloniaProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AnimatedMenu, bool>(nameof(IsOpen));

    private readonly IDisposable _disposable;

    public bool IsOpen
    {
        get => (bool)(GetValue(IsOpenProperty) ?? false);
        set => SetValue(IsOpenProperty, value);
    }
    
    protected AnimatedMenu()
    {
        // Subscribe to changes in the IsOpen property
        _disposable = this.GetObservable(IsOpenProperty).ToObservable()
            .Skip(1)
            .SubscribeAwait(async (isOpen, _) =>
            {
                // Make sure it is visible before starting the animation
                if (!IsVisible && isOpen)
                {
                    IsVisible = true;
                }

                await DoAnimation(isOpen).ConfigureAwait(true);

                // Set the visibility so that it is not interactable while closed
                if (!isOpen)
                {
                    IsVisible = false;
                }
                
            }, DebugHelper.LogError(nameof(AnimatedMenu), nameof(IsOpenProperty)));
    }
    
    /// <summary>
    /// Performs an animation to change the opacity of the control based on the value of the isOpen parameter.
    /// </summary>
    /// <param name="isOpen">A boolean value indicating whether the animation should open or close the control.</param>
    /// <returns>A Task representing the asynchronous operation of the animation.</returns>
    private async Task DoAnimation(bool isOpen)
    {
        var from = isOpen ? 0 : 1;
        var to = isOpen ? 1 : 0;
        const double speed = 0.3;
        var anim = AnimationsHelper.OpacityAnimation(from, to, speed);
        await anim.RunAsync(this).ConfigureAwait(true);
    }
    
    public virtual void Dispose()
    {
        _disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}
