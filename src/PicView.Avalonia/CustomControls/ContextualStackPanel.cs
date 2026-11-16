using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A stack panel whose contextual children, shown only now and then, never widen it when it stacks vertically: its
/// width is set by its other children, and the contextual ones are measured within that width, so they wrap or fit
/// rather than grow the panel. Stacked horizontally it is a plain stack panel.
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
        if (Orientation != Orientation.Vertical)
        {
            return base.MeasureOverride(availableSize);
        }

        var children = Children;
        double width = 0, height = 0;
        var shown = 0;

        var permanent = new Size(availableSize.Width, double.PositiveInfinity);
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
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
                shown++;
            }
        }

        var contextual = new Size(Math.Min(width, availableSize.Width), double.PositiveInfinity);
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
                height += child.DesiredSize.Height;
                shown++;
            }
        }

        if (shown > 1)
        {
            height += Spacing * (shown - 1);
        }

        return new Size(width, height);
    }
}
