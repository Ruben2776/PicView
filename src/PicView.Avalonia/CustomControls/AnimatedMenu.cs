using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia;
using PicView.Avalonia.Animation;
using PicView.Avalonia.Helpers;
using ReactiveUI;

namespace PicView.Avalonia.CustomControls;
public class AnimatedMenu : UserControl
{
    public static readonly AvaloniaProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AnimatedMenu, bool>(nameof(IsOpen));

    public bool IsOpen
    {
        get => (bool)(GetValue(IsOpenProperty) ?? false);
        set => SetValue(IsOpenProperty, value);
    }
    
    protected AnimatedMenu()
    {
        // Subscribe to changes in the IsOpen property
        this.WhenAnyValue(x => x.IsOpen)
            .Select(async isOpen =>
            {
                // Make sure it is visible before starting the animation
                if (!IsVisible && isOpen)
                {
                    IsVisible = true;
                }

                await DoAnimation(isOpen);

                // Set the visibility so that it is not interactable while closed
                if (!isOpen)
                {
                    IsVisible = false;
                }
            })
            .Subscribe();
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
        var anim = AnimationsHelper.OpacityAnimation(from, to, 0.3);
        await anim.RunAsync(this);
    }
}
