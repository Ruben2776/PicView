using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace PicView.Editing.Crop.Tools
{
    internal class CropShape
    {
        public Shape Shape { get; }

        private readonly Canvas? originalCanvas;

        public CropShape(Shape shape, Canvas? overlayCanvas = null)
        {
            Shape = shape;
            originalCanvas = overlayCanvas;
        }

        public void Redraw(double newX, double newY, double newWidth, double newHeight)
        {
            if (newHeight < 0 || newWidth < 0) return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                UpdateRectangle(newX, newY, newWidth, newHeight);
            }
            else
            {
                RedrawSolidShape(newX, newY, newWidth, newHeight);
            }
        }

        private void RedrawSolidShape(double newX, double newY, double newWidth, double newHeight)
        {
            Canvas.SetLeft(Shape, newX);
            Canvas.SetTop(Shape, newY);
            Shape.Width = newWidth;
            Shape.Height = newHeight;
        }

        public void UpdateRectangle(double newX, double newY, double newWidth, double newHeight)
        {
            if (newHeight < 0 || newWidth < 0) { return; }

            Canvas.SetLeft(Shape, newX);
            Canvas.SetTop(Shape, newY);
            Shape.Height = newHeight;
            Shape.Width = newHeight;

            if (Shape.Height > originalCanvas.ActualWidth)
            {
                Canvas.SetLeft(Shape, 0);
                Shape.Height = originalCanvas.ActualWidth;
                Shape.Width = originalCanvas.ActualWidth;
            }

            if (Shape.Height + newX > originalCanvas.ActualWidth)
            {
                Canvas.SetLeft(Shape, originalCanvas.ActualWidth - Shape.ActualWidth);
            }
        }
    }
}