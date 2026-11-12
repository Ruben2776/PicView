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
            control.ShownImageRect());
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

    /// <summary>
    /// Where <paramref name="selection"/> goes when the view changes under it, so all of it lies in
    /// <paramref name="visible"/>: a side that fits is moved in at its length, as <see cref="ClampMove"/> moves it,
    /// and a side longer than the visible part is cut to the part of it in view, which keeps an edge already in view
    /// where it is. A side longer than the visible part with none of it in view takes the whole visible part.
    /// </summary>
    internal static Rect ClampIntoView(Rect selection, Rect visible)
    {
        var (x, width) = ClampSpan(selection.X, selection.Width, visible.X, visible.Right);
        var (y, height) = ClampSpan(selection.Y, selection.Height, visible.Y, visible.Bottom);
        return new Rect(x, y, width, height);
    }

    private static (double Start, double Length) ClampSpan(double start, double length, double lowest, double highest)
    {
        if (length <= highest - lowest)
        {
            return (Math.Max(lowest, Math.Min(highest - length, start)), length);
        }

        var from = Math.Max(lowest, start);
        var to = Math.Min(highest, start + length);
        return to > from ? (from, to - from) : (lowest, highest - lowest);
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