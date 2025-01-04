using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.Crop;
public class CropDragHandler(CropControl control)
{
    private Point _dragStart;
    private bool _isDragging;
    private Rect _originalRect;
    
    private const double ButtonLeftOffset = 11;
    private const double ButtonTopOffset = 3;

    public void OnDragStart(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed || 
            control.DataContext is not ImageCropperViewModel vm)
        {
            return;
        }

        _dragStart = e.GetPosition(control.RootCanvas);
        
        // Get current left and top values; ensure they are initialized
        var currentLeft = Canvas.GetLeft(control.MainRectangle);
        var currentTop = Canvas.GetTop(control.MainRectangle);

        // Set default values if NaN
        if (double.IsNaN(currentLeft))
        {
            currentLeft = 0;
        }

        if (double.IsNaN(currentTop))
        {
            currentTop = 0;
        }

        _originalRect = new Rect(currentLeft, currentTop, vm.SelectionWidth, vm.SelectionHeight);
        _isDragging = true;
    }

    public void OnDragMove(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (control.DataContext is not ImageCropperViewModel vm)
        {
            return;
        }

        if (!_isDragging)
        {
            return;
        }

        var currentPos = e.GetPosition(control.RootCanvas); // Ensure it's relative to RootCanvas
        var delta = currentPos - _dragStart;

        // Calculate new left and top positions, ensure _originalRect is valid
        var newLeft = _originalRect.X + delta.X;
        var newTop = _originalRect.Y + delta.Y;

        // Clamp the newLeft and newTop values to keep the rectangle within bounds
        newLeft = Math.Max(0, Math.Min(vm.ImageWidth - vm.SelectionWidth, newLeft));
        newTop = Math.Max(0, Math.Min(vm.ImageHeight - vm.SelectionHeight, newTop));

        // Only proceed if new positions are valid (i.e., not NaN)
        if (double.IsNaN(newLeft) || double.IsNaN(newTop))
        {
            return;
        }

        // Update the main rectangle's position
        Canvas.SetLeft(control.MainRectangle, newLeft);
        Canvas.SetTop(control.MainRectangle, newTop);
        
        Canvas.SetLeft(control.SizeBorder, newLeft + ButtonLeftOffset);
        Canvas.SetTop(control.SizeBorder, newTop - control.SizeBorder.Bounds.Height - ButtonTopOffset);

        // Update view model values
        vm.SelectionX = Convert.ToInt32(newLeft);
        vm.SelectionY = Convert.ToInt32(newTop);
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