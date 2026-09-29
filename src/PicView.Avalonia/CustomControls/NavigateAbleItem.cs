using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace PicView.Avalonia.CustomControls;

[PseudoClasses(PseudoCurrentItem, PseudoSelectedItem, PseudoContextMenuOpen)]
public class NavigateAbleItem : ContentControl
{
    private const string PseudoCurrentItem = ":currentItem";
    private const string PseudoSelectedItem = ":selectedItem";
    private const string PseudoContextMenuOpen = ":contextMenuOpen";
    
    protected override Type StyleKeyOverride => typeof(NavigateAbleItem);

    public void SetCurrent(bool isCurrent)
    {
        PseudoClasses.Set(PseudoCurrentItem, isCurrent);
    }
    
    public void SetSelected(bool isSelected)
    {
        PseudoClasses.Set(PseudoSelectedItem, isSelected);
    }
    
    public void SetContextMenuOpen(bool isOpen)
    {
        PseudoClasses.Set(PseudoContextMenuOpen, isOpen);
    }
}