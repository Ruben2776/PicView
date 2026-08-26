using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using PicView.Core.Gallery;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A virtualizing wrap panel for image galleries supporting variable item widths with fixed item height,
/// horizontal or vertical orientation, and container recycling.
/// </summary>
public class VirtualizingGallery: VirtualizingPanel, IScrollSnapPointsInfo
{
    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<WrapPanel, Orientation>(nameof(Orientation), defaultValue: Orientation.Horizontal);
    /// <inheritdoc cref="WrapPanel" />
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
    
    /// <summary>
    /// The fixed item height of all items.
    /// <remarks>Must be greater than zero. The widths are calculated dynamically.</remarks>
    /// </summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }
    
    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemHeight), GalleryDefaults.DefaultDockedGalleryHeight);

    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemSpacing));

    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(LineSpacing));
    
    /// <inheritdoc cref="WrapPanel" />
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    /// <inheritdoc cref="WrapPanel" />
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }
    
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size constraint)
    {
        var itemWidth = double.NaN;
        var itemHeight = ItemHeight;
        var itemSpacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        var orientation = Orientation;
        var children = Children;
        var curLineSize = new UVSize(orientation);
        var panelSize = new UVSize(orientation);
        var uvConstraint = new UVSize(orientation, constraint.Width, constraint.Height);
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);
        var itemExists = false;
        var lineExists = false;
        var useLayoutRounding = UseLayoutRounding;

        var childConstraint = new Size(
            itemWidthSet ? itemWidth : constraint.Width,
            itemHeightSet ? itemHeight : constraint.Height);

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];
            // Flow passes its own constraint to children
            child.Measure(childConstraint);

            var childSize = new UVSize(orientation,
                itemWidthSet ? itemWidth : child.DesiredSize.Width,
                itemHeightSet ? itemHeight : child.DesiredSize.Height);

            var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
            if (GreaterThan(useLayoutRounding, curLineSize.U + childSize.U + nextSpacing, uvConstraint.U)) // Need to switch to another line
            {
                panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);
                curLineSize = childSize;

                itemExists = child.IsVisible;
                lineExists = true;
            }
            else // Continue to accumulate a line
            {
                curLineSize.U += childSize.U + nextSpacing;
                curLineSize.V = Math.Max(childSize.V, curLineSize.V);

                itemExists |= child.IsVisible; // keep true
            }
        }

        // The last line size, if any should be added
        panelSize.U = Math.Max(curLineSize.U, panelSize.U);
        panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);

        return new Size(panelSize.Width, panelSize.Height);
    }
    
    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = double.NaN;
        var itemHeight = ItemHeight;
        var itemSpacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        var orientation = Orientation;
        var isHorizontal = orientation == Orientation.Horizontal;
        var children = Children;
        var firstInLine = 0;
        double accumulatedV = 0;
        var itemU = isHorizontal ? itemWidth : itemHeight;
        var curLineSize = new UVSize(orientation);
        var uvFinalSize = new UVSize(orientation, finalSize.Width, finalSize.Height);
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);
        var itemExists = false;
        var lineExists = false;
        var useLayoutRounding = UseLayoutRounding;

        for (var i = 0; i < children.Count; ++i)
        {
            var child = children[i];
            var childSize = new UVSize(orientation,
                itemWidthSet ? itemWidth : child.DesiredSize.Width,
                itemHeightSet ? itemHeight : child.DesiredSize.Height);

            var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
            if (GreaterThan(useLayoutRounding, curLineSize.U + childSize.U + nextSpacing, uvFinalSize.U)) // Need to switch to another line
            {
                accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                ArrangeLine(curLineSize.V, firstInLine, i);
                accumulatedV += curLineSize.V; // add the height of the line just arranged
                curLineSize = childSize;

                firstInLine = i;

                itemExists = child.IsVisible;
                lineExists = true;
            }
            else // Continue to accumulate a line
            {
                curLineSize.U += childSize.U + nextSpacing;
                curLineSize.V = Math.Max(childSize.V, curLineSize.V);

                itemExists |= child.IsVisible; // keep true
            }
        }

        // Arrange the last line, if any
        if (firstInLine < children.Count)
        {
            accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
            ArrangeLine(curLineSize.V, firstInLine, children.Count);
        }

        return finalSize;

        void ArrangeLine(double lineV, int start, int endExcluded)
        {
            var useItemU = isHorizontal ? itemWidthSet : itemHeightSet;
            var u = 0d;
            // Count of spacings between items
            const double stretchRatio = 1d;

            for (var i = start; i < endExcluded; ++i)
            {
                var layoutSlotU = GetChildU(i) * stretchRatio;
                children[i].Arrange(isHorizontal ?
                    new Rect(u, accumulatedV, layoutSlotU, lineV) :
                    new Rect(accumulatedV, u, lineV, layoutSlotU));
                u += layoutSlotU + (children[i].IsVisible ? itemSpacing : 0);
            }

            return;
            double GetChildU(int i) => useItemU ? itemU :
                isHorizontal ? children[i].DesiredSize.Width : children[i].DesiredSize.Height;
        }
    }
        
        
    private struct UVSize
    {
        internal UVSize(Orientation orientation, double width, double height)
        {
            U = V = 0d;
            _orientation = orientation;
            Width = width;
            Height = height;
        }

        internal UVSize(Orientation orientation)
        {
            U = V = 0d;
            _orientation = orientation;
        }

        internal double U;
        internal double V;
        private Orientation _orientation;

        internal double Width
        {
            get => _orientation == Orientation.Horizontal ? U : V;
            set { if (_orientation == Orientation.Horizontal) U = value; else V = value; }
        }

        internal double Height
        {
            get => _orientation == Orientation.Horizontal ? V : U;
            set { if (_orientation == Orientation.Horizontal) V = value; else U = value; }
        }
    }
    
    
    private static bool GreaterThan(bool useLayoutRounding, double value1, double value2)
    {
        return useLayoutRounding
            ? value1 > value2 && value1 - value2 > LayoutHelper.LayoutEpsilon
            : GreaterThan(value1, value2);
    }
    public static bool GreaterThan(double value1, double value2) => value1 > value2 && !AreClose(value1, value2);
    internal const double DoubleEpsilon = 2.2204460492503131e-016;
    public static bool AreClose(double value1, double value2)
    {
        //in case they are Infinities (then epsilon check does not work)
        if (value1 == value2)
        {
            return true;
        }
        var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DoubleEpsilon;
        var delta = value1 - value2;
        return -eps < delta && eps > delta;
    }

    
    protected override Control ScrollIntoView(int index)
    {
        throw new NotImplementedException();
    }

    protected override Control ContainerFromIndex(int index)
    {
        throw new NotImplementedException();
    }

    protected override int IndexFromContainer(Control container)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<Control> GetRealizedContainers()
    {
        throw new NotImplementedException();
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
    {
        throw new NotImplementedException();
    }

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        throw new NotImplementedException();
    }

    public bool AreHorizontalSnapPointsRegular { get; set; }
    public bool AreVerticalSnapPointsRegular { get; set; }
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;
}