using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.Crop;

public interface IResizeStrategy
{
    void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm);
}

public static class CropResizeStrategyFactory
{
    public static IResizeStrategy Create(CropResizeMode mode)
    {
        return mode switch
        {
            CropResizeMode.TopLeft => new TopLeftResizeStrategy(),
            CropResizeMode.TopRight => new TopRightResizeStrategy(),
            CropResizeMode.BottomLeft => new BottomLeftResizeStrategy(),
            CropResizeMode.BottomRight => new BottomRightResizeStrategy(),
            CropResizeMode.Left => new LeftResizeStrategy(),
            CropResizeMode.Right => new RightResizeStrategy(),
            CropResizeMode.Top => new TopResizeStrategy(),
            CropResizeMode.Bottom => new BottomResizeStrategy(),
            _ => throw new ArgumentException($"Unsupported resize mode: {mode}")
        };
    }
}

public class BottomResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        // Get the current mouse position relative to RootCanvas
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        // Calculate the new height based on vertical movement
        var newHeight = originalRect.Height + delta.Y;

        // Ensure the new height doesn't go negative or too small
        newHeight = Math.Max(newHeight, 1);

        // Ensure the bottom edge doesn't go beyond the canvas bounds
        if (originalRect.Y + newHeight > control.RootCanvas.Bounds.Height)
        {
            newHeight = control.RootCanvas.Bounds.Height - originalRect.Y;
        }

        // Prevent the size from becoming too small
        newHeight = Math.Max(newHeight, 1);

        // Update the view model with the new height
        vm.SelectionHeight = newHeight;

        // Update the rectangle on the canvas
        Canvas.SetLeft(control.MainRectangle, originalRect.X);
        Canvas.SetTop(control.MainRectangle, originalRect.Y);
    }
}

public class TopResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        // Get the current mouse position relative to RootCanvas
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        // Calculate the new top position and height
        var newTop = originalRect.Y + delta.Y;
        var newHeight = originalRect.Height - delta.Y;

        // Ensure the new height doesn't go negative or too small
        if (newHeight < 1)
        {
            newHeight = 1;
            newTop = originalRect.Y + (originalRect.Height - 1); // Adjust top to preserve min height
        }

        // Ensure the top edge doesn't go beyond the canvas bounds
        if (newTop < 0)
        {
            newTop = 0;
            newHeight = originalRect.Height + originalRect.Y; // Adjust height to compensate
        }

        // Prevent the size from becoming too small
        newHeight = Math.Max(newHeight, 1);

        // Update the view model with the new top and height
        vm.SelectionHeight = newHeight;
        vm.SelectionY = Convert.ToInt32(newTop);

        // Update the rectangle on the canvas
        Canvas.SetLeft(control.MainRectangle, originalRect.X);
        Canvas.SetTop(control.MainRectangle, newTop);
    }
}

public class RightResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        // Get the current mouse position relative to RootCanvas
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        // Calculate the new width based on horizontal movement
        var newWidth = originalRect.Width + delta.X;

        // Ensure the new width doesn't go beyond the bounds of the RootCanvas
        if (originalRect.X + newWidth > vm.ImageWidth)
        {
            newWidth = vm.ImageWidth - originalRect.X;
        }

        // Constrain the width to a minimum value (e.g., 1 to prevent zero or negative width)
        newWidth = Math.Max(newWidth, 1);

        // Update the view model with the new width
        vm.SelectionWidth = newWidth;

        // Update the rectangle on the canvas
        Canvas.SetLeft(control.MainRectangle, originalRect.X);
        Canvas.SetTop(control.MainRectangle, originalRect.Y);
    }
}

public class LeftResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
// Get the current mouse position relative to RootCanvas
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        // Calculate the new X position
        var newX = originalRect.X + delta.X;

        // Calculate the new width based on horizontal movement
        var newWidth = originalRect.Width - delta.X;

        // Ensure that the rectangle doesn't go beyond the right boundary
        if (newX < 0)
        {
            newWidth = originalRect.Width + originalRect.X;
            newX = 0;
        }

        // Ensure the new width doesn't go negative or too small
        newWidth = Math.Max(newWidth, 1);

        // Update the view model with the new X position and width
        vm.SelectionX = Convert.ToInt32(newX);
        vm.SelectionWidth = newWidth;

        // Update the rectangle on the canvas
        Canvas.SetLeft(control.MainRectangle, newX);
        Canvas.SetTop(control.MainRectangle, originalRect.Y);
    }
}

public class BottomRightResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        // Calculate the new width and height based on the drag delta
        var newWidth = originalRect.Width + delta.X;
        var newHeight = originalRect.Height + delta.Y;

        // If shift is pressed, maintain square aspect ratio
        if (MainKeyboardShortcuts.ShiftDown)
        {
            // Use the larger of the two dimensions to maintain square shape
            var size = Math.Max(newWidth, newHeight);
            newWidth = size;
            newHeight = size;
        }

        // Ensure the new width and height do not exceed the image bounds
        var newRight = originalRect.X + newWidth;
        var newBottom = originalRect.Y + newHeight;

        if (newRight > vm.ImageWidth)
        {
            newWidth = vm.ImageWidth - originalRect.X;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newHeight = newWidth;
            }
        }

        if (newBottom > vm.ImageHeight)
        {
            newHeight = vm.ImageHeight - originalRect.Y;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newWidth = newHeight;
            }
        }

        // Constrain the minimum size
        newWidth = Math.Max(newWidth, 1);
        newHeight = Math.Max(newHeight, 1);

        // Apply the new width and height
        vm.SelectionWidth = newWidth;
        vm.SelectionHeight = newHeight;
        
        // Update the rectangle on the canvas
        Canvas.SetLeft(control.MainRectangle, originalRect.X);
        Canvas.SetTop(control.MainRectangle, originalRect.Y);
    }
}

public class BottomLeftResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        var newWidth = originalRect.Width - delta.X;
        var newHeight = originalRect.Height + delta.Y;
        var newX = originalRect.X + delta.X;

        // If shift is pressed, maintain square aspect ratio
        if (MainKeyboardShortcuts.ShiftDown)
        {
            // Use the larger dimension to determine the square size
            var size = Math.Max(newWidth, newHeight);
            // Calculate how much to adjust X to maintain the right edge
            var widthDiff = size - newWidth;
            newX -= widthDiff;
            newWidth = size;
            newHeight = size;
        }

        // Ensure the left doesn't move beyond the left edge
        if (newX < 0)
        {
            var adjustment = -newX;
            newX = 0;
            newWidth -= adjustment;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newHeight = newWidth;
            }
        }

        // Ensure the height doesn't exceed the canvas' bottom edge
        if (originalRect.Y + newHeight > vm.ImageHeight)
        {
            newHeight = vm.ImageHeight - originalRect.Y;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newWidth = newHeight;
                newX = originalRect.X + (originalRect.Width - newWidth);
            }
        }

        // Prevent the width and height from becoming too small
        newWidth = Math.Max(newWidth, 1);
        newHeight = Math.Max(newHeight, 1);

        // Apply the new size and position
        vm.SelectionX = Convert.ToInt32(newX);
        vm.SelectionWidth = newWidth;
        vm.SelectionHeight = newHeight;
        Canvas.SetLeft(control.MainRectangle, newX);
        Canvas.SetTop(control.MainRectangle, originalRect.Y);
    }
}

public class TopRightResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        var newWidth = originalRect.Width + delta.X;
        var newHeight = originalRect.Height - delta.Y;
        var newY = originalRect.Y + delta.Y;

        // If shift is pressed, maintain square aspect ratio
        if (MainKeyboardShortcuts.ShiftDown)
        {
            // Use the larger dimension to determine the square size
            var size = Math.Max(newWidth, newHeight);
            // Calculate how much to adjust Y to maintain the bottom edge
            var heightDiff = size - newHeight;
            newY -= heightDiff;
            newWidth = size;
            newHeight = size;
        }

        // Ensure the width doesn't exceed the canvas' right edge
        if (originalRect.X + newWidth > vm.ImageWidth)
        {
            newWidth = vm.ImageWidth - originalRect.X;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newHeight = newWidth;
                newY = originalRect.Y + (originalRect.Height - newHeight);
            }
        }

        // Ensure the top doesn't move above the top edge of the canvas
        if (newY < 0)
        {
            var adjustment = -newY;
            newY = 0;
            newHeight = originalRect.Height + adjustment;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newWidth = newHeight;
            }
        }

        // Prevent the size from becoming too small
        newHeight = Math.Max(newHeight, 1);
        newWidth = Math.Max(newWidth, 1);

        // Apply the new size and position
        vm.SelectionY = Convert.ToInt32(newY);
        vm.SelectionWidth = newWidth;
        vm.SelectionHeight = newHeight;
        Canvas.SetLeft(control.MainRectangle, originalRect.X);
        Canvas.SetTop(control.MainRectangle, newY);
    }
}

public class TopLeftResizeStrategy : IResizeStrategy
{
    public void Resize(CropControl control, PointerEventArgs e, Point resizeStart, Rect originalRect,
        ImageCropperViewModel vm)
    {
        var currentPos = e.GetPosition(control.RootCanvas);
        var delta = currentPos - resizeStart;

        var newWidth = originalRect.Width - delta.X;
        var newHeight = originalRect.Height - delta.Y;
        var newLeft = originalRect.X + delta.X;
        var newTop = originalRect.Y + delta.Y;

        // If shift is pressed, maintain square aspect ratio
        if (MainKeyboardShortcuts.ShiftDown)
        {
            // Use the larger dimension to determine the square size
            var size = Math.Max(newWidth, newHeight);
            // Calculate adjustments needed to maintain right and bottom edges
            var widthDiff = size - newWidth;
            var heightDiff = size - newHeight;
            newLeft -= widthDiff;
            newTop -= heightDiff;
            newWidth = size;
            newHeight = size;
        }

        // Ensure we don't go beyond boundaries
        if (newLeft < 0)
        {
            var adjustment = -newLeft;
            newLeft = 0;
            newWidth -= adjustment;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newHeight = newWidth;
                newTop = originalRect.Y + (originalRect.Height - newHeight);
            }
        }

        if (newTop < 0)
        {
            var adjustment = -newTop;
            newTop = 0;
            newHeight -= adjustment;
            if (MainKeyboardShortcuts.ShiftDown)
            {
                newWidth = newHeight;
                newLeft = originalRect.X + (originalRect.Width - newWidth);
            }
        }

        // Prevent the size from becoming too small
        newHeight = Math.Max(newHeight, 1);
        newWidth = Math.Max(newWidth, 1);

        // Apply the new size and position
        vm.SelectionX = Convert.ToInt32(newLeft);
        vm.SelectionY = Convert.ToInt32(newTop);
        vm.SelectionWidth = newWidth;
        vm.SelectionHeight = newHeight;
        Canvas.SetLeft(control.MainRectangle, newLeft);
        Canvas.SetTop(control.MainRectangle, newTop);
    }
}