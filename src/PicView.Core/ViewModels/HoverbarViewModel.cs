using R3;

namespace PicView.Core.ViewModels;

public class HoverbarViewModel
{
    public bool IsHoverRotateLeftClicked { get; set; }
    public bool IsHoverRotateRightClicked { get; set; }

    public bool IsHoverNavigationButtonNextClicked { get; set; }
    public bool IsHoverNavigationButtonPreviousClicked { get; set; }

    public double MaxWidth { get; set; } = 775;
    public BindableReactiveProperty<bool> IsHoverbarVisible { get; } = new();
}