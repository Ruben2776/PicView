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
        
        // Clamp the newLeft and newTop values to keep the rectangle within bounds
        newLeft = Math.Max(0, Math.Min(vm.ImageWidth.CurrentValue - _originalRect.Width, newLeft));
        newTop = Math.Max(0, Math.Min(vm.ImageHeight.CurrentValue - _originalRect.Height, newTop));
        
        // Only proceed if new positions are valid (i.e., not NaN)
        if (double.IsNaN(newLeft) || double.IsNaN(newTop))
        {
            return;
        }
        
        // The layout manager places the rectangle and the label from the selection
        tab.Crop.SetSelection(newLeft, newTop, _originalRect.Width, _originalRect.Height);
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