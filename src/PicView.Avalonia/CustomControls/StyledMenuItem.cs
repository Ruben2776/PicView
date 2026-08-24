using Avalonia.Controls;

namespace PicView.Avalonia.CustomControls;

public class StyledMenuItem : MenuItem
{
    protected override Type StyleKeyOverride => typeof(MenuItem);
    public StyledMenuItem()
    {
        if (Settings.Theme.Dark)
        {
            Classes.Add("AccentHighLight");
            return;
        }
        Classes.Add("SubtleHighLight");
    }
}
