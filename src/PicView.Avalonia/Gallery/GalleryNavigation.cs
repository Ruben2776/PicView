using Avalonia;
using Avalonia.Controls;

namespace PicView.Avalonia.Gallery;

public static class GalleryNavigation
{
    public static void CenterScrollToSelectedItem(ListBox listBox, ListBoxItem selectedItem)
    {
        var p = selectedItem.TranslatePoint(new Point(), listBox);
        listBox.Scroll.Offset = new Vector(-selectedItem.Bounds.X, -selectedItem.Bounds.Y);
    }
}