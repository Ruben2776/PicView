using Avalonia.Controls;
using Avalonia.Input;

namespace PicView.Avalonia.CustomControls;
public class CustomSlider : Slider
{
    protected override Type StyleKeyOverride => typeof(Slider);

    public CustomSlider()
    {
        PointerWheelChanged += CustomSlider_PointerWheelChanged;
    }

    private void CustomSlider_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var indexChange = e.Delta.Y > 0 ? TickFrequency : -TickFrequency;
        Value += indexChange;
    }
}
