using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.Crop;

public class CropResizeHandler(CropControl control)
{
    private bool _isResizing;
    private Rect _originalRect;
    private Point _resizeStart;

    public void OnResizeStart(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed ||
            control.DataContext is not ImageCropperViewModel vm)
        {
            return;
        }

        _resizeStart = e.GetPosition(control.RootCanvas);
        _originalRect = new Rect(Canvas.GetLeft(control.MainRectangle), Canvas.GetTop(control.MainRectangle), vm.SelectionWidth,
            vm.SelectionHeight);
        _isResizing = true;
    }

    public void OnResizeMove(object? sender, PointerEventArgs e, CropResizeMode mode)
    {
        if (!_isResizing || control.DataContext is not ImageCropperViewModel vm)
        {
            return;
        }

        var resizer = CropResizeStrategyFactory.Create(mode);
        resizer.Resize(control, e, _resizeStart, _originalRect, vm);
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