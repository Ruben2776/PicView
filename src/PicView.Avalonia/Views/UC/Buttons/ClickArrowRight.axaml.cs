using Avalonia.Controls;
using PicView.Avalonia.Animations;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class ClickArrowRight : UserControl
{
    public ClickArrowRight()
    {
        InitializeComponent();
        PolyButton.PointerEntered += delegate
        {
            Opacity = 1;
            PolyButton.Opacity = 1;
        };
        PointerEntered += async delegate
        {
            await DoClickArrowAnimation(true, PolyButton);
        };
        PointerExited += async delegate
        {
            await DoClickArrowAnimation(false, PolyButton);
        };
    }
    
    private static bool _isClickArrowAnimationRunning;
    private static async Task DoClickArrowAnimation(bool isShown, Control control)
    {
        if (_isClickArrowAnimationRunning)
        {
            return;
        }
        _isClickArrowAnimationRunning = true;
        var from = isShown ? 0 : 1;
        var to = isShown ? 1 : 0;
        var anim = AnimationsHelper.OpacityAnimation(from, to, 0.3);
        await anim.RunAsync(control);
        _isClickArrowAnimationRunning = false;
    }
}
