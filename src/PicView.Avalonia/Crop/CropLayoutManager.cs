using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Crop;

public class CropLayoutManager(CropControl control)
{
    private const int DefaultSelectionSize = 200;

    public void InitializeLayout(MainWindowViewModel vm)
    {
        if (control.DataContext is not TabViewModel tab || tab.Crop is null)
        {
            return;
        }

        // The window fits the crop before the control lays out, so a size not known yet means there is no image to crop
        var imageWidth = vm.ImageWidth.CurrentValue;
        var imageHeight = vm.ImageHeight.CurrentValue;
        if (double.IsNaN(imageWidth) || double.IsNaN(imageHeight))
        {
            return;
        }

        // In the part of the image the view shows, which is all of it unless the crop opened zoomed in
        var visible = control.VisibleSurfaceRect();
        if (visible.Width <= 0 || visible.Height <= 0)
        {
            visible = new Rect(0, 0, imageWidth, imageHeight);
        }

        var selection = DefaultSelection(visible);
        tab.Crop.SetSelection(selection.X, selection.Y, selection.Width, selection.Height);

        UpdateLayout();
    }

    /// <summary>
    /// The selection a crop opens with: <see cref="DefaultSelectionSize"/> a side, or half of a side of
    /// <paramref name="visible"/> too short to hold twice that, in the centre of <paramref name="visible"/>.
    /// </summary>
    internal static Rect DefaultSelection(Rect visible)
    {
        var width = visible.Width >= DefaultSelectionSize * 2 ? DefaultSelectionSize : visible.Width / 2;
        var height = visible.Height >= DefaultSelectionSize * 2 ? DefaultSelectionSize : visible.Height / 2;
        return new Rect(visible.X + (visible.Width - width) / 2, visible.Y + (visible.Height - height) / 2, width,
            height);
    }

    public void UpdateLayout()
    {
        try
        {
            if (control.DataContext is TabViewModel { Crop: { } crop })
            {
                Canvas.SetLeft(control.MainRectangle, crop.SelectionX.Value);
                Canvas.SetTop(control.MainRectangle, crop.SelectionY.Value);
            }

            UpdateButtonPositions();
            UpdateDimming();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(CropLayoutManager), nameof(UpdateLayout), e);
        }
    }

    /// <summary>Dims everything but the selection.</summary>
    private void UpdateDimming()
    {
        if (control.DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.Crop == null)
        {
            return;
        }
        
        var vm = tab.ParentWindowContext;
        var image = new Rect(0, 0, vm.ImageWidth.CurrentValue, vm.ImageHeight.CurrentValue);
        if (double.IsNaN(image.Width) || double.IsNaN(image.Height))
        {
            return;
        }

        // One shape with the selection as its hole. Four rectangles side by side showed seams where they met once
        // the selection lay at fractions of a view unit, as it does on image pixels
        var selection = new Rect(tab.Crop.SelectionX.Value, tab.Crop.SelectionY.Value, tab.Crop.SelectionWidth.Value,
            tab.Crop.SelectionHeight.Value);
        control.Dimming.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { new RectangleGeometry(image), new RectangleGeometry(selection) }
        };
    }

    // A handle's box on the canvas; it is drawn at this size on screen whatever the zoom
    private const double HandleSize = 20;

    // The gap on screen between the top left handle and the size label beside it
    private const double LabelGap = 2;

    /// <summary>
    /// Places the eight handles on the selection's corners and sides, and the size label above its top left
    /// corner, all inside the part of the image the view shows.
    /// </summary>
    public void UpdateButtonPositions()
    {
        if (control.DataContext is not TabViewModel { Crop: { } crop })
        {
            return;
        }

        var left = crop.SelectionX.Value;
        var top = crop.SelectionY.Value;
        var right = left + crop.SelectionWidth.Value;
        var bottom = top + crop.SelectionHeight.Value;
        var centreX = left + crop.SelectionWidth.Value / 2;
        var centreY = top + crop.SelectionHeight.Value / 2;

        var visible = control.VisibleSurfaceRect();
        var scale = control.ZoomPanControl.Scale;

        // A side's handle slides along its side to stay in view, since it moves only that side
        var alongX = Within(centreX, visible.X, visible.Right);
        var alongY = Within(centreY, visible.Y, visible.Bottom);

        PlaceAndShow(control.TopLeftButton, new Point(left, top), visible, scale);
        PlaceAndShow(control.TopMiddleButton, new Point(alongX, top), visible, scale);
        PlaceAndShow(control.TopRightButton, new Point(right, top), visible, scale);
        PlaceAndShow(control.LeftMiddleButton, new Point(left, alongY), visible, scale);
        PlaceAndShow(control.RightMiddleButton, new Point(right, alongY), visible, scale);
        PlaceAndShow(control.BottomLeftButton, new Point(left, bottom), visible, scale);
        PlaceAndShow(control.BottomMiddleButton, new Point(alongX, bottom), visible, scale);
        PlaceAndShow(control.BottomRightButton, new Point(right, bottom), visible, scale);

        // The label keeps its place by the top left corner, in view, whether or not that corner's handle shows
        var topLeft = PlaceHandle(new Point(left, top), visible, scale);
        var handleCentre = new Point(topLeft.X + HandleSize / 2, topLeft.Y + HandleSize / 2);
        Place(control.SizeBorder, PlaceLabel(handleCentre, control.SizeBorder.Bounds.Size, visible, scale));
    }

    private static void PlaceAndShow(Control handle, Point point, Rect visible, double scale)
    {
        handle.IsVisible = ShowsHandle(point, visible);
        Place(handle, PlaceHandle(point, visible, scale));
    }

    private static void Place(Control element, Point position)
    {
        Canvas.SetLeft(element, position.X);
        Canvas.SetTop(element, position.Y);
    }

    /// <summary>
    /// Where a handle's box goes on the canvas so the handle is centred on <paramref name="point"/>, moved inwards
    /// where it would leave <paramref name="visible"/>. The handle is drawn at one over <paramref name="scale"/>
    /// of its box about the box's centre, so it keeps its size on screen.
    /// </summary>
    internal static Point PlaceHandle(Point point, Rect visible, double scale)
    {
        var half = HandleSize / 2 / scale;
        return new Point(
            Within(point.X, visible.X + half, visible.Right - half) - HandleSize / 2,
            Within(point.Y, visible.Y + half, visible.Bottom - half) - HandleSize / 2);
    }

    /// <summary>
    /// Where the size label's box goes on the canvas: beside the top left handle, whose center is
    /// <paramref name="handleCenter"/>, and above it, moved inwards where it would leave <paramref name="visible"/>.
    /// The label is drawn at one over <paramref name="scale"/> of <paramref name="label"/> from the bottom left of
    /// its box, so it keeps its size on screen.
    /// </summary>
    internal static Point PlaceLabel(Point handleCenter, Size label, Rect visible, double scale)
    {
        var shownWidth = label.Width / scale;
        var shownHeight = label.Height / scale;

        var shownLeft = Within(handleCenter.X + (HandleSize / 2 + LabelGap) / scale, visible.X,
            visible.Right - shownWidth);
        // A handle's height above the handle, or at the top of the view when that is higher up
        var shownTop = Within(handleCenter.Y - HandleSize * 1.5 / scale, visible.Y, visible.Bottom - shownHeight);

        return new Point(shownLeft, shownTop + shownHeight - label.Height);
    }

    /// <summary>
    /// Whether a handle at <paramref name="point"/> is shown: only when its point is in <paramref name="visible"/>.
    /// A handle drawn at the view's edge for a selection edge that is out of view would not sit on the edge it
    /// moves, so that handle is hidden until the edge is in view.
    /// </summary>
    internal static bool ShowsHandle(Point point, Rect visible) =>
        point.X >= visible.X - Epsilon && point.X <= visible.Right + Epsilon &&
        point.Y >= visible.Y - Epsilon && point.Y <= visible.Bottom + Epsilon;

    // Slack for the arithmetic of a point that lies on an edge
    private const double Epsilon = 1e-6;

    /// <summary>
    /// <paramref name="visible"/> drawn in to whole image pixels, each <paramref name="pixel"/> view units wide: what
    /// a selection, which sits on whole pixels, may reach without an edge lying outside the view. With no whole
    /// pixel in view it is <paramref name="visible"/> itself.
    /// </summary>
    internal static Rect SnapInward(Rect visible, double pixel)
    {
        if (pixel is <= 0 or double.NaN)
        {
            return visible;
        }

        var left = Math.Ceiling(visible.X / pixel - Epsilon) * pixel;
        var top = Math.Ceiling(visible.Y / pixel - Epsilon) * pixel;
        var right = Math.Floor(visible.Right / pixel + Epsilon) * pixel;
        var bottom = Math.Floor(visible.Bottom / pixel + Epsilon) * pixel;
        return right > left && bottom > top ? new Rect(left, top, right - left, bottom - top) : visible;
    }

    // The lowest wins when there is no room, so what is placed starts inside the view
    private static double Within(double value, double lowest, double highest) =>
        Math.Max(lowest, Math.Min(highest, value));
}