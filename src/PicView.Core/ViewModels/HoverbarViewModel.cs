using R3;

namespace PicView.Core.ViewModels;

public class HoverbarViewModel
{
    public double MaxWidth { get; set; } = 775;
    public BindableReactiveProperty<bool> IsHoverbarVisible { get; } = new(false);
}