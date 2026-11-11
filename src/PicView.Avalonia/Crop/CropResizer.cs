using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Crop;

public static class CropResizer
{
    public static void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        MainWindowViewModel vm, CropViewModel cropViewModel, CropResizeMode mode)
    {
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;
    
        // Initialize with original values
        var newX = originalRect.X;
        var newY = originalRect.Y;
        var newWidth = originalRect.Width;
        var newHeight = originalRect.Height;
    
        // Calculate new dimensions based on resize mode
        switch (mode)
        {
            case CropResizeMode.TopLeft:
                newX += delta.X;
                newY += delta.Y;
                newWidth -= delta.X;
                newHeight -= delta.Y;
                break;
    
            case CropResizeMode.TopRight:
                newY += delta.Y;
                newWidth += delta.X;
                newHeight -= delta.Y;
                break;
    
            case CropResizeMode.BottomLeft:
                newX += delta.X;
                newWidth -= delta.X;
                newHeight += delta.Y;
                break;
    
            case CropResizeMode.BottomRight:
                newWidth += delta.X;
                newHeight += delta.Y;
                break;
    
            case CropResizeMode.Left:
                newX += delta.X;
                newWidth -= delta.X;
                break;
    
            case CropResizeMode.Right:
                newWidth += delta.X;
                break;
    
            case CropResizeMode.Top:
                newY += delta.Y;
                newHeight -= delta.Y;
                break;
    
            case CropResizeMode.Bottom:
                newHeight += delta.Y;
                break;
        }
    
        // Handle aspect ratio constraints (maintain square when Shift is pressed)
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && IsCornerMode(mode))
        {
            // Use the larger dimension to determine square size
            var size = Math.Max(newWidth, newHeight);
    
            // Adjust position based on resize mode to maintain the correct anchor point
            switch (mode)
            {
                case CropResizeMode.TopLeft:
                    newX = originalRect.X + originalRect.Width - size;
                    newY = originalRect.Y + originalRect.Height - size;
                    break;
    
                case CropResizeMode.TopRight:
                    newY = originalRect.Y + originalRect.Height - size;
                    break;
    
                case CropResizeMode.BottomLeft:
                    newX = originalRect.X + originalRect.Width - size;
                    break;
            }
    
            newWidth = size;
            newHeight = size;
        }
    
        // Keep the rectangle in the part of the image the view shows
        var bounds = ResizeBounds(originalRect, control.VisibleSurfaceRect(),
            new Rect(0, 0, vm.ImageWidth.CurrentValue, vm.ImageHeight.CurrentValue));
        var constrained = Constrain(new Rect(newX, newY, Math.Max(newWidth, 0), Math.Max(newHeight, 0)), bounds);
        newX = constrained.X;
        newY = constrained.Y;
        newWidth = constrained.Width;
        newHeight = constrained.Height;

        // Update the view model
        try
        {
            // The layout manager places the rectangle from the selection
            cropViewModel.SetSelection(newX, newY, newWidth, newHeight);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(CropResizer), nameof(Resize), exception);
        }
    }
    
    private static bool IsCornerMode(CropResizeMode mode) => mode is
        CropResizeMode.TopLeft or
        CropResizeMode.TopRight or
        CropResizeMode.BottomLeft or
        CropResizeMode.BottomRight;

    /// <summary>
    /// What a resize may reach: the part of the image the view shows, <paramref name="visible"/>, which is all of it
    /// until the crop is zoomed in, widened to the selection's own edges as the resize began. An edge already off
    /// screen then stays where it is while another edge is dragged.
    /// </summary>
    internal static Rect ResizeBounds(Rect original, Rect visible, Rect image) =>
        original.Union(visible).Intersect(image);

    /// <summary>
    /// <paramref name="wanted"/> kept inside <paramref name="bounds"/> and at least one unit each way. An edge
    /// pushed past the bounds stops there and the opposite edge stays where it was.
    /// </summary>
    internal static Rect Constrain(Rect wanted, Rect bounds)
    {
        var x = wanted.X;
        var y = wanted.Y;
        var width = wanted.Width;
        var height = wanted.Height;

        if (x < bounds.X)
        {
            width -= bounds.X - x;
            x = bounds.X;
        }

        if (y < bounds.Y)
        {
            height -= bounds.Y - y;
            y = bounds.Y;
        }

        if (x >= bounds.Right)
        {
            x = bounds.Right - 1;
        }

        if (y >= bounds.Bottom)
        {
            y = bounds.Bottom - 1;
        }

        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        if (x + width > bounds.Right)
        {
            width = bounds.Right - x;
        }

        if (y + height > bounds.Bottom)
        {
            height = bounds.Bottom - y;
        }

        return new Rect(x, y, width, height);
    }
}