using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A stack panel whose contextual children, shown only now and then, never thicken it: its width when it stacks
/// vertically, or its height when it stacks horizontally, is set by its other children, and the contextual ones are
/// measured within it, so they wrap or fit rather than grow the panel.
/// </summary>
public class ContextualStackPanel : StackPanel
{
    public static readonly AttachedProperty<bool> IsContextualProperty =
        AvaloniaProperty.RegisterAttached<ContextualStackPanel, Control, bool>("IsContextual");

    static ContextualStackPanel()
    {
        AffectsParentMeasure<ContextualStackPanel>(IsContextualProperty);
    }

    // Styled as a stack panel
    protected override Type StyleKeyOverride => typeof(StackPanel);

    public static bool GetIsContextual(Control control) => control.GetValue(IsContextualProperty);

    public static void SetIsContextual(Control control, bool value) => control.SetValue(IsContextualProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        // Along is the stacking direction, across the thickness the permanent children set
        var vertical = Orientation == Orientation.Vertical;
        var availableAcross = vertical ? availableSize.Width : availableSize.Height;
        var children = Children;
        double across = 0, along = 0;
        var shown = 0;

        var permanent = Oriented(availableAcross, double.PositiveInfinity, vertical);
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (GetIsContextual(child))
            {
                continue;
            }

            child.Measure(permanent);
            if (child.IsVisible)
            {
                across = Math.Max(across, vertical ? child.DesiredSize.Width : child.DesiredSize.Height);
                along += vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
                shown++;
            }
        }

        var contextual = Oriented(Math.Min(across, availableAcross), double.PositiveInfinity, vertical);
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (!GetIsContextual(child))
            {
                continue;
            }

            child.Measure(contextual);
            if (child.IsVisible)
            {
                along += vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
                shown++;
            }
        }

        if (shown > 1)
        {
            along += Spacing * (shown - 1);
        }

        return Oriented(across, along, vertical);
    }

    // A contextual child is arranged no thicker than the permanent children, even when it would like to be
    protected override Size ArrangeOverride(Size finalSize)
    {
        var vertical = Orientation == Orientation.Vertical;
        var children = Children;
        double offset = 0;
        var first = true;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (!child.IsVisible)
            {
                continue;
            }

            if (!first)
            {
                offset += Spacing;
            }

            first = false;
            var along = vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
            child.Arrange(vertical
                ? new Rect(0, offset, finalSize.Width, along)
                : new Rect(offset, 0, along, finalSize.Height));
            offset += along;
        }

        return finalSize;
    }

    private static Size Oriented(double across, double along, bool vertical) =>
        vertical ? new Size(across, along) : new Size(along, across);
}
