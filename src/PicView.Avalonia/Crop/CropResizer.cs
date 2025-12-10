using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Crop;

public static class CropResizer
{
    public static void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm, CropResizeMode mode)
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
        if (MainKeyboardShortcuts.ShiftDown && IsCornerMode(mode))
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

        // Apply bounds constraints
        ApplyBoundsConstraints(ref newX, ref newY, ref newWidth, ref newHeight, vm.ImageWidth.CurrentValue, vm.ImageHeight.CurrentValue);

        // Update the view model
        try
        {
            vm.SelectionX.Value = Convert.ToInt32(newX);
            vm.SelectionY.Value = Convert.ToInt32(newY);
            vm.SetSelectionWidth(Convert.ToUInt32(newWidth));
            vm.SetSelectionHeight(Convert.ToUInt32(newHeight));
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(CropResizer), nameof(Resize), exception);
        }

        // Update the rectangle position on canvas
        Canvas.SetLeft(control.MainRectangle, newX);
        Canvas.SetTop(control.MainRectangle, newY);
    }

    private static bool IsCornerMode(CropResizeMode mode) => mode is
        CropResizeMode.TopLeft or
        CropResizeMode.TopRight or
        CropResizeMode.BottomLeft or
        CropResizeMode.BottomRight;

    private static void ApplyBoundsConstraints(ref double x, ref double y, ref double width, ref double height, double maxWidth, double maxHeight)
    {
        // Ensure we don't go beyond canvas bounds
        if (x < 0)
        {
            width += x;
            x = 0;
        }

        if (y < 0)
        {
            height += y;
            y = 0;
        }

        if (x >= maxWidth)
        {
            x = maxWidth - 1;
        }

        if (y >= maxHeight)
        {
            y = maxHeight - 1;
        }

        // Ensure minimum dimensions
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        // Ensure we don't exceed the canvas bounds
        if (x + width > maxWidth)
        {
            width = maxWidth - x;
        }

        if (y + height > maxHeight)
        {
            height = maxHeight - y;
        }
    }
}