using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;
public class GalleryListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);
    
    public GalleryListBox()
    {
        SelectionMode = SelectionMode.Single;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Disable control from hijacking keys
        e.Handled = true;
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        // Disable control from hijacking keys
        e.Handled = true;
    }
    
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS already has horizontal scrolling for touchpad
            return;
        }
        var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        const int speed = 34;

        if (e.Delta.Y > 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.Offset -= new Vector(speed, 0);
            }
            else
            {
                scrollViewer.Offset -= new Vector(-speed, 0);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.Offset -= new Vector(-speed, 0);
            }
            else
            {
                scrollViewer.Offset -= new Vector(speed, 0);
            }
        }
    }
}
