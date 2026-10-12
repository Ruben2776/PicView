using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace PicView.Avalonia.Views.UC;

public partial class FavoriteItem : UserControl
{
    public FavoriteItem()
    {
        InitializeComponent();
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = false;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        PointerEntered -= OnPointerEntered;
        PointerExited -= OnPointerExited;
    }
}