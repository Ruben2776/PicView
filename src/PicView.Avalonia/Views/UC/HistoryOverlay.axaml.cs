using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace PicView.Avalonia.Views.UC;

public partial class HistoryOverlay : UserControl
{
    private Point? _dragStart;
    public HistoryOverlay()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved   += OnPointerMoved;
        PointerReleased+= (_, __) => _dragStart = null;
    }

    private void OnPointerPressed(object? s, PointerPressedEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        this.Focus();
        e.Handled = true;
    }

    private void OnPointerMoved(object? s, PointerEventArgs e)
    {
        if (_dragStart is null) return;
        var parent = this.VisualRoot as Window ?? this.GetVisualParent<Window>();
        if (parent is null) return;

        var p = e.GetPosition(parent);
        var dx = p.X - _dragStart.Value.X;
        var dy = p.Y - _dragStart.Value.Y;

        // Move within a Canvas host
        if (Parent is Canvas canvas)
        {
            Canvas.SetLeft(this, dx);
            Canvas.SetTop(this,  dy);
        }
    }
}
