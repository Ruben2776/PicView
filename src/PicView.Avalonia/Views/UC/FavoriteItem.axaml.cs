using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace PicView.Avalonia.Views.UC;

public partial class FavoriteItem : UserControl
{
    public FavoriteItem()
    {
        InitializeComponent();
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        RemoveButton.Click += HideControl;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = false;
    }

    private void HideControl(object? sender, RoutedEventArgs e)
    {
        // The command already handles the actual removal,
        // so we just hide it so that the user doesn't think it is bugged
        IsVisible = false;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        PointerEntered -= OnPointerEntered;
        PointerExited -= OnPointerExited;
        RemoveButton.Click -= HideControl;
    }
}