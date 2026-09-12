using Avalonia.Controls;

namespace PicView.Avalonia.CustomControls;

public class StyledMenu : Menu
{
    protected override Type StyleKeyOverride => typeof(Menu);
    public StyledMenu()
    {
        if (Settings.Theme.Dark)
        {
            Classes.Add("AccentHighLight");
            return;
        }
        Classes.Add("SubtleHighLight");
    }
}
