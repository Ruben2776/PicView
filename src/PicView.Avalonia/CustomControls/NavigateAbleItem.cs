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

    protected bool IsCurrent { get; private set; }

    protected bool IsSelected { get; private set; }

    protected bool IsContextMenuOpen { get; private set; }

    public void SetCurrent(bool isCurrent)
    {
        if (IsCurrent == isCurrent)
        {
            return;
        }

        IsCurrent = isCurrent;
        PseudoClasses.Set(PseudoCurrentItem, isCurrent);
        OnMarkChanged();
    }

    public void SetSelected(bool isSelected)
    {
        if (IsSelected == isSelected)
        {
            return;
        }

        IsSelected = isSelected;
        PseudoClasses.Set(PseudoSelectedItem, isSelected);
        OnMarkChanged();
    }

    public void SetContextMenuOpen(bool isOpen)
    {
        if (IsContextMenuOpen == isOpen)
        {
            return;
        }

        IsContextMenuOpen = isOpen;
        PseudoClasses.Set(PseudoContextMenuOpen, isOpen);
        OnMarkChanged();
    }

    /// <summary>Called after the current, selected or context-menu mark changes.</summary>
    protected virtual void OnMarkChanged()
    {
    }
}
