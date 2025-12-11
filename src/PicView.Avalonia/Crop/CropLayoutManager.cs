using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Crop;

public class CropLayoutManager(CropControl control)
{
    private const int DefaultSelectionSize = 200;

    public void InitializeLayout()
    {
        if (control.DataContext is not MainViewModel vm)
        {
            return;
        }

        // Ensure image dimensions are valid before proceeding
        if (vm.PicViewer.ImageWidth.CurrentValue <= 0 || vm.PicViewer.ImageHeight.CurrentValue <= 0)
        {
            return;
        }

        // Set initial width and height for the crop rectangle
        var originalWidth = vm.PicViewer.ImageWidth.CurrentValue >= DefaultSelectionSize * 2
            ? DefaultSelectionSize
            : (uint)(vm.PicViewer.ImageWidth.CurrentValue / 2);
        var originalHeight = vm.PicViewer.ImageHeight.CurrentValue >= DefaultSelectionSize * 2
            ? DefaultSelectionSize
            : (uint)(vm.PicViewer.ImageHeight.CurrentValue / 2);

        vm.Crop.SetSelectionWidth(originalWidth);
        vm.Crop.SetSelectionHeight(originalHeight);

        // Calculate centered position
        vm.Crop.SelectionX.Value = Convert.ToInt32((vm.PicViewer.ImageWidth.CurrentValue - vm.Crop.SelectionWidth.CurrentValue) / 2);
        vm.Crop.SelectionY.Value = Convert.ToInt32((vm.PicViewer.ImageHeight.CurrentValue - vm.Crop.SelectionHeight.CurrentValue) / 2);

        // Apply the calculated position to the MainRectangle
        Canvas.SetLeft(control.MainRectangle, vm.Crop.SelectionX.CurrentValue);
        Canvas.SetTop(control.MainRectangle, vm.Crop.SelectionY.CurrentValue);

        UpdateLayout();
    }

    public void UpdateLayout()
    {
        try
        {
            UpdateButtonPositions();
            UpdateSurroundingRectangles();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(CropLayoutManager), nameof(UpdateLayout), e);
        }
    }

    private void UpdateSurroundingRectangles()
    {
        if (control.DataContext is not MainViewModel vm)
        {
            return;
        }

        // Converting to int fixes black border
        var left = Convert.ToInt32(Canvas.GetLeft(control.MainRectangle));
        var top = Convert.ToInt32(Canvas.GetTop(control.MainRectangle));
        var right = Convert.ToInt32(left + vm.Crop.SelectionWidth.CurrentValue);
        var bottom = Convert.ToInt32(top + vm.Crop.SelectionHeight.CurrentValue);

        // Calculate the positions and sizes for the surrounding rectangles
        // Top Rectangle (above MainRectangle)
        control.TopRectangle.Width = vm.PicViewer.ImageWidth.CurrentValue;
        control.TopRectangle.Height = top < 0 ? 0 : top;
        Canvas.SetTop(control.TopRectangle, 0);

        // Bottom Rectangle (below MainRectangle)
        control.BottomRectangle.Width = vm.PicViewer.ImageWidth.CurrentValue;
        var newBottomRectangleHeight = vm.PicViewer.ImageHeight.CurrentValue - bottom < 0 ? 0 : vm.PicViewer.ImageHeight.CurrentValue - bottom;
        control.BottomRectangle.Height = newBottomRectangleHeight;
        Canvas.SetTop(control.BottomRectangle, bottom);

        // Left Rectangle (left of MainRectangle)
        control.LeftRectangle.Width = left < 0 ? 0 : left;
        control.LeftRectangle.Height = vm.Crop.SelectionHeight.CurrentValue;
        Canvas.SetLeft(control.LeftRectangle, 0);
        Canvas.SetTop(control.LeftRectangle, top);

        // Right Rectangle (right of MainRectangle)
        var newRightRectangleWidth = vm.PicViewer.ImageWidth.CurrentValue - right < 0 ? 0 : vm.PicViewer.ImageWidth.CurrentValue - right;
        control.RightRectangle.Width = newRightRectangleWidth;
        control.RightRectangle.Height = vm.Crop.SelectionHeight.CurrentValue;
        Canvas.SetLeft(control.RightRectangle, right);
        Canvas.SetTop(control.RightRectangle, top);
    }

    public void UpdateButtonPositions()
    {
        if (control.DataContext is not MainViewModel vm)
        {
            return;
        }

        var selectionX = vm.Crop.SelectionX.CurrentValue;
        var selectionY = vm.Crop.SelectionY.CurrentValue;
        var selectionWidth = vm.Crop.SelectionWidth.CurrentValue;
        var selectionHeight = vm.Crop.SelectionHeight.CurrentValue;

        // Get the bounds of the RootCanvas (the control container)
        const int rootCanvasLeft = 0;
        const int rootCanvasTop = 0;
        var rootCanvasRight = control.RootCanvas.Bounds.Width;
        var rootCanvasBottom = control.RootCanvas.Bounds.Height;

        // Calculate the positions for each button
        var topLeftX = selectionX - control.TopLeftButton.Width / 2;
        var topLeftY = selectionY - control.TopLeftButton.Height / 2;

        var topRightX = selectionX + selectionWidth - control.TopRightButton.Width / 2;
        var topRightY = selectionY - control.TopRightButton.Height / 2;

        var topMiddleX = selectionX + selectionWidth / 2 - control.TopMiddleButton.Width / 2;
        var topMiddleY = selectionY - control.TopMiddleButton.Height / 2;

        var bottomLeftX = selectionX - control.BottomLeftButton.Width / 2;
        var bottomLeftY = selectionY + selectionHeight - control.BottomLeftButton.Height / 2;

        var bottomRightX = selectionX + selectionWidth - control.BottomRightButton.Width / 2;
        var bottomRightY = selectionY + selectionHeight - control.BottomRightButton.Height / 2;

        var bottomMiddleX = selectionX + selectionWidth / 2 - control.BottomMiddleButton.Width / 2;
        var bottomMiddleY = selectionY + selectionHeight - control.BottomMiddleButton.Height / 2;

        var leftMiddleX = selectionX - control.LeftMiddleButton.Width / 2;
        var leftMiddleY = selectionY + selectionHeight / 2 - control.LeftMiddleButton.Height / 2;

        var rightMiddleX = selectionX + selectionWidth - control.RightMiddleButton.Width / 2;
        var rightMiddleY = selectionY + selectionHeight / 2 - control.RightMiddleButton.Height / 2;

        // Ensure buttons stay within RootCanvas bounds (by clamping positions)
        topLeftX = Math.Max(rootCanvasLeft, Math.Min(rootCanvasRight - control.TopLeftButton.Width, topLeftX));
        topLeftY = Math.Max(rootCanvasTop, Math.Min(rootCanvasBottom - control.TopLeftButton.Height, topLeftY));

        topRightX = Math.Max(rootCanvasLeft, Math.Min(rootCanvasRight - control.TopRightButton.Width, topRightX));
        topRightY = Math.Max(rootCanvasTop, Math.Min(rootCanvasBottom - control.TopRightButton.Height, topRightY));

        topMiddleX = Math.Max(rootCanvasLeft, Math.Min(rootCanvasRight - control.TopMiddleButton.Width, topMiddleX));
        topMiddleY = Math.Max(rootCanvasTop, Math.Min(rootCanvasBottom - control.TopMiddleButton.Height, topMiddleY));

        bottomLeftX = Math.Max(rootCanvasLeft, Math.Min(rootCanvasRight - control.BottomLeftButton.Width, bottomLeftX));
        bottomLeftY = Math.Max(rootCanvasTop,
            Math.Min(rootCanvasBottom - control.BottomLeftButton.Height, bottomLeftY));

        bottomRightX = Math.Max(rootCanvasLeft,
            Math.Min(rootCanvasRight - control.BottomRightButton.Width, bottomRightX));
        bottomRightY = Math.Max(rootCanvasTop,
            Math.Min(rootCanvasBottom - control.BottomRightButton.Height, bottomRightY));

        bottomMiddleX = Math.Max(rootCanvasLeft,
            Math.Min(rootCanvasRight - control.BottomMiddleButton.Width, bottomMiddleX));
        bottomMiddleY = Math.Max(rootCanvasTop,
            Math.Min(rootCanvasBottom - control.BottomMiddleButton.Height, bottomMiddleY));

        leftMiddleX = Math.Max(rootCanvasLeft, Math.Min(rootCanvasRight - control.LeftMiddleButton.Width, leftMiddleX));
        leftMiddleY = Math.Max(rootCanvasTop,
            Math.Min(rootCanvasBottom - control.LeftMiddleButton.Height, leftMiddleY));

        rightMiddleX = Math.Max(rootCanvasLeft,
            Math.Min(rootCanvasRight - control.RightMiddleButton.Width, rightMiddleX));
        rightMiddleY = Math.Max(rootCanvasTop,
            Math.Min(rootCanvasBottom - control.RightMiddleButton.Height, rightMiddleY));

        // Set the final button positions
        Canvas.SetLeft(control.TopLeftButton, topLeftX);
        Canvas.SetTop(control.TopLeftButton, topLeftY);

        Canvas.SetLeft(control.TopRightButton, topRightX);
        Canvas.SetTop(control.TopRightButton, topRightY);

        Canvas.SetLeft(control.TopMiddleButton, topMiddleX);
        Canvas.SetTop(control.TopMiddleButton, topMiddleY);

        Canvas.SetLeft(control.BottomLeftButton, bottomLeftX);
        Canvas.SetTop(control.BottomLeftButton, bottomLeftY);

        Canvas.SetLeft(control.BottomRightButton, bottomRightX);
        Canvas.SetTop(control.BottomRightButton, bottomRightY);

        Canvas.SetLeft(control.BottomMiddleButton, bottomMiddleX);
        Canvas.SetTop(control.BottomMiddleButton, bottomMiddleY);

        Canvas.SetLeft(control.LeftMiddleButton, leftMiddleX);
        Canvas.SetTop(control.LeftMiddleButton, leftMiddleY);

        Canvas.SetLeft(control.RightMiddleButton, rightMiddleX);
        Canvas.SetTop(control.RightMiddleButton, rightMiddleY);

        Canvas.SetLeft(control.SizeBorder, topLeftX + control.TopLeftButton.Bounds.Width + 2);
        Canvas.SetTop(control.SizeBorder, Math.Max(0, topLeftY - control.TopLeftButton.Bounds.Height));
    }
}