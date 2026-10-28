using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Crop;

public class CropResizeHandler(CropControl control)
{
    private bool _isResizing;
    private Rect _originalRect;
    private Point _resizeStart;

    public void OnResizeStart(PointerPressedEventArgs e)
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

        _resizeStart = e.GetPosition(control.RootCanvas);
        _originalRect = new Rect(Canvas.GetLeft(control.MainRectangle), Canvas.GetTop(control.MainRectangle), tab.Crop.SelectionWidth.CurrentValue,
            tab.Crop.SelectionHeight.CurrentValue);
        _isResizing = true;
    }

    public void OnResizeMove(object? sender, PointerEventArgs e, CropResizeMode mode)
    {
        if (!_isResizing || control.DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.Crop == null)
        {
            return;
        }

        CropResizer.Resize(control, e, _resizeStart, _originalRect, tab.ParentWindowContext, tab.Crop, mode);
    }

    public void OnResizeEnd(object? sender, PointerReleasedEventArgs e)
    {
        Reset();
    }

    public void Reset()
    {
        _isResizing = false;
    }
}