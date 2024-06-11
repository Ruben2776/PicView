using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

public class PanAndZoomBorder : Border
{
    protected override Type StyleKeyOverride => typeof(Border);
    
    private Point? _start;
    private double _scale = 1.0;
    
    private TranslateTransform? _translateTransform;
    private ScaleTransform? _scaleTransform;
    
    private Control? _parentControl;
    private Control? _childControl;

    public void Initialize(Control parentControl, Control childControl)
    {
        _parentControl = parentControl;
        _childControl = childControl;
        
        _translateTransform = new TranslateTransform();
        _scaleTransform = new ScaleTransform();
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_scaleTransform);
        transformGroup.Children.Add(_translateTransform);
        RenderTransform = transformGroup;
        
        // Subscribe to pointer events for panning
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        // Subscribe to mouse wheel event for zooming
        _parentControl.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, handledEventsToo: true);
        _parentControl.PointerPressed += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                ResetZoom();
            }
        };
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ResetZoom();
            return;
        }
        _start = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!Equals(e.Pointer.Captured, this))
        {
            return;
        }
        Pan(e);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Equals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var animate = true; // TODO replace with setting one day
        Zoom(e, animate);
        e.Handled = true;
    }

    #region Pan

    private void Pan(PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        var delta = position - _start;
        if (!delta.HasValue)
        {
            return;
        }
        var x = _translateTransform.X + delta.Value.X;
        var y =_translateTransform.Y + delta.Value.Y;
        _start = position;
        _translateTransform.X = x;
        _translateTransform.Y = y;
        InvalidateVisual();
    }

    #endregion

    #region Zoom
    
    public void Zoom(PointerWheelEventArgs e, bool animate = true)
    {
        var delta = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;

        var position = e.GetPosition(_parentControl);
        RenderTransformOrigin = new RelativePoint(position, RelativeUnit.Absolute);
        var relativeX = position.X / Bounds.Width;
        var relativeY = position.Y / Bounds.Height;

        var newScale = _scale * delta;

        // Prevent zooming out too much
        if (newScale < 0.1) return;

        _scale = newScale;
        _scaleTransform.ScaleX = relativeX * Bounds.Width;
        _scaleTransform.ScaleY = relativeY * Bounds.Height;
        _scaleTransform.ScaleX = _scale;
        _scaleTransform.ScaleY = _scale;
        InvalidateVisual();
    }

    private void ResetZoom()
    {
        _scale = 1.0;
        _scaleTransform.ScaleX = 1.0;
        _scaleTransform.ScaleY = 1.0;
        _translateTransform.X = 0;
        _translateTransform.Y = 0;
        InvalidateVisual();
    }

    #endregion

    
}
