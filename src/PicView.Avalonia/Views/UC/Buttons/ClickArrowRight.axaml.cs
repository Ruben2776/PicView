using Avalonia.Controls;
using PicView.Avalonia.Animations;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class ClickArrowRight : UserControl
{
    public ClickArrowRight()
    {
        InitializeComponent();
        PointerEntered += async (_, _) => await DoAnimation(true);
        PointerExited += async (_, _) => await DoAnimation(false);
    }

    private async Task DoAnimation(bool isOpen)
    {
        var from = isOpen ? 0 : 1;
        var to = isOpen ? 1 : 0;
        var anim = AnimationsHelper.OpacityAnimation(from, to, 0.3);
        await anim.RunAsync(this);
    }
}
