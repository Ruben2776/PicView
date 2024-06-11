using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace PicView.Avalonia.ImageTransformations;
/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder : ILogicalScrollable
{
    private Size _extent;
    private Size _viewport;
    private Vector _offset;
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private EventHandler? _scrollInvalidated;

    /// <summary>
    /// Calculate scrollable properties.
    /// </summary>
    /// <param name="source">The source bounds.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(Rect source, Matrix matrix, out Size extent, out Size viewport, out Vector offset)
    {
        var bounds = new Rect(0, 0, source.Width, source.Height);
            
        viewport = bounds.Size;

        var transformed = bounds.TransformToAABB(matrix);

        Log($"[CalculateScrollable] source: {source}, bounds: {bounds}, transformed: {transformed}");

        var width = transformed.Size.Width;
        var height = transformed.Size.Height;

        if (width < viewport.Width)
        {
            width = viewport.Width;

            if (transformed.Position.X < 0.0)
            {
                width += Math.Abs(transformed.Position.X);
            }
            else
            {
                var widthTranslated = transformed.Size.Width + transformed.Position.X;
                if (widthTranslated > width)
                {
                    width += widthTranslated - width;
                }
            }
        }
        else if (!(width > viewport.Width))
        {
            width += Math.Abs(transformed.Position.X);
        }
            
        if (height < viewport.Height)
        {
            height = viewport.Height;
                
            if (transformed.Position.Y < 0.0)
            {
                height += Math.Abs(transformed.Position.Y);
            }
            else
            {
                var heightTranslated = transformed.Size.Height + transformed.Position.Y;
                if (heightTranslated > height)
                {
                    height += heightTranslated - height;
                }
            }
        }
        else if (!(height > viewport.Height))
        {
            height += Math.Abs(transformed.Position.Y);
        }

        extent = new Size(width, height);

        var ox = transformed.Position.X;
        var oy = transformed.Position.Y;

        var offsetX = ox < 0 ? Math.Abs(ox) : 0;
        var offsetY = oy < 0 ? Math.Abs(oy) : 0;

        offset = new Vector(offsetX, offsetY);

        Log($"[CalculateScrollable] Extent: {extent} | Offset: {offset} | Viewport: {viewport}");
    }

    /// <inheritdoc/>
    Size IScrollable.Extent => _extent;

    /// <inheritdoc/>
    Vector IScrollable.Offset
    {
        get => _offset;
        set
        {
            Log($"[Offset] offset value: {value}");
            if (_updating)
            {
                return;
            }
            _updating = true;

            var (x, y) = _offset;
            var dx = x - value.X;
            var dy = y - value.Y;

            _offset = value;

            Log($"[Offset] offset: {_offset}, dx: {dx}, dy: {dy}");

            _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
            Invalidate(!this.IsPointerOver);

            _updating = false;
        }
    }

    /// <inheritdoc/>
    Size IScrollable.Viewport => _viewport;

    bool ILogicalScrollable.CanHorizontallyScroll
    {
        get => _canHorizontallyScroll;
        set
        {
            _canHorizontallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.CanVerticallyScroll
    {
        get => _canVerticallyScroll;
        set
        {
            _canVerticallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => _scrollInvalidated += value;
        remove => _scrollInvalidated -= value;
    }

    Size ILogicalScrollable.ScrollSize => new Size(1, 1);

    Size ILogicalScrollable.PageScrollSize => new Size(10, 10);

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        _scrollInvalidated?.Invoke(this, e);
    }

    private void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        if (_element == null)
        {
            return;
        }

        CalculateScrollable(_element.Bounds, _matrix, out var extent, out var viewport, out var offset);

        Log($"[InvalidateScrollable] _element.Bounds: {_element.Bounds}, _matrix: {_matrix}");
        Log($"[InvalidateScrollable] _extent: {_extent}, extent: {extent}, diff: {extent - _extent}");
        Log($"[InvalidateScrollable] _offset: {_offset}, offset: {offset}, diff: {offset - _offset}");
        Log($"[InvalidateScrollable] _viewport: {_viewport}, viewport: {viewport}, diff: {viewport - _viewport}");

        _extent = extent;
        _offset = offset;
        _viewport = viewport;

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
    }
}
