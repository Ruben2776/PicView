using R3;

namespace PicView.Core.ViewModels;

public class ToolTipViewModel
{
    public BindableReactiveProperty<string?> ToolTipMessageSource { get; } = new();
    public BindableReactiveProperty<bool> ToolTipMessageCentered { get; } = new();
    public ReactiveProperty<TimeSpan> ToolTipMessageInterval { get; } = new(TimeSpan.FromSeconds(2));
}