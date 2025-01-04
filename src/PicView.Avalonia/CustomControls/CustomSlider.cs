using Avalonia.Controls;
using Avalonia.Input;
using PicView.Core.Config;

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
        if (SettingsHelper.Settings.Zoom.IsUsingTouchPad)
        {
            // Don't scroll unintentionally
            return;
        }

        double indexChange;
        if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
        {
            indexChange = e.Delta.Y > 0 ? -TickFrequency : TickFrequency;
        }
        else
        {
            indexChange = e.Delta.Y < 0 ? -TickFrequency : TickFrequency;
        }
        Value += indexChange;
    }
}
