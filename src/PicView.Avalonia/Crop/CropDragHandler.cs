using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Crop;
public class CropDragHandler(CropControl control)
{
    private Point _dragStart;
    private bool _isDragging;
    private Rect _originalRect;

    public void OnDragStart(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed || 
            control.DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.Crop == null)
        {
            return;
        }
        
        _dragStart = e.GetPosition(control.RootCanvas);
        _originalRect = new Rect(tab.Crop.SelectionX.Value, tab.Crop.SelectionY.Value, tab.Crop.SelectionWidth.Value,
            tab.Crop.SelectionHeight.Value);
        _isDragging = true;
    }

    public void OnDragMove(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }
        
        if (control.DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.Crop == null)
        {
            return;
        }
        
        if (!_isDragging)
        {
            return;
        }
        
        var vm = tab.ParentWindowContext;
        if (vm == null)
        {
            return;
        }
        var currentPos = e.GetPosition(control.RootCanvas); // Ensure it's relative to RootCanvas
        var delta = currentPos - _dragStart;
        
        // Calculate new left and top positions, ensure _originalRect is valid
        var newLeft = _originalRect.X + delta.X;
        var newTop = _originalRect.Y + delta.Y;
        
        // Keep the rectangle in the part of the image the view shows
        var clamped = ClampMove(_originalRect, new Point(newLeft, newTop), control.SelectionBounds(),
            new Rect(0, 0, vm.ImageWidth.CurrentValue, vm.ImageHeight.CurrentValue));
        newLeft = clamped.X;
        newTop = clamped.Y;

        // Only proceed if new positions are valid (i.e., not NaN)
        if (double.IsNaN(newLeft) || double.IsNaN(newTop))
        {
            return;
        }
        
        // The layout manager places the rectangle and the label from the selection
        tab.Crop.SetSelection(newLeft, newTop, _originalRect.Width, _originalRect.Height);
    }

    /// <summary>
    /// Where a selection of <paramref name="selection"/>'s size lands when moved to <paramref name="wanted"/>: inside
    /// <paramref name="visible"/>, the part of the image the view shows, which is all of it until the crop is zoomed
    /// in. A side longer than the visible part cannot fit it, so that axis keeps to <paramref name="image"/>.
    /// </summary>
    internal static Point ClampMove(Rect selection, Point wanted, Rect visible, Rect image)
    {
        var horizontal = selection.Width <= visible.Width ? visible : image;
        var vertical = selection.Height <= visible.Height ? visible : image;
        return new Point(
            Math.Max(horizontal.X, Math.Min(horizontal.Right - selection.Width, wanted.X)),
            Math.Max(vertical.Y, Math.Min(vertical.Bottom - selection.Height, wanted.Y)));
    }

    public void OnDragEnd(object? sender, PointerReleasedEventArgs e)
    {
        Reset();
    }

    public void Reset()
    {
        _isDragging = false;
    }
}