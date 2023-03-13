using PicView.Editing.Crop.State;
using PicView.Editing.Crop.Tools;
using PicView.UILogic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PicView.Editing.Crop
{
    public class CropArea
    {
        internal readonly Size OriginalSize;
        internal readonly Rect CroppedRectAbsolute;

        public CropArea(Size originalSize, Rect croppedRectAbsolute)
        {
            OriginalSize = originalSize;
            CroppedRectAbsolute = croppedRectAbsolute;
        }
    }

    public class CropService
    {
        private readonly CropAdorner cropAdorner;
        private readonly Canvas? canvas;
        private readonly CropTool? cropTool;

        private IToolState currentToolState;
        private readonly IToolState createState;
        private readonly IToolState dragState;
        private readonly IToolState completeState;

        public Adorner Adorner => cropAdorner;

        private enum TouchPoint
        {
            OutsideRectangle,
            InsideRectangle
        }

        public CropService(FrameworkElement? adornedElement)
        {
            if (adornedElement == null)
            {
                return;
            }

            // Make sure menus are hidden when cropping
            UC.Close_UserControls();

            // Show context menu
            _ = new CropContextMenu();

            canvas = new Canvas
            {
                Height = adornedElement.ActualHeight,
                Width = adornedElement.ActualWidth
            };

            cropAdorner = new CropAdorner(adornedElement, canvas);
            var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);

#if DEBUG
            Debug.Assert(adornerLayer != null, nameof(adornerLayer) + " != null");
#endif

            adornerLayer.Add(cropAdorner);

            var cropShape = new CropShape(
                new Rectangle
                {
                    Height = 4,
                    Width = 4,
                    Stroke = (SolidColorBrush)Application.Current.Resources["MainColorBrush"],
                    StrokeThickness = 2
                }
            );

            cropTool = new CropTool(canvas);
            createState = new CreateState(cropTool, canvas);
            completeState = new CompleteState();
            dragState = new DragState(cropTool, canvas);

            currentToolState = completeState;

            cropAdorner.PreviewMouseLeftButtonDown += AdornerOnMouseLeftButtonDown;
            cropAdorner.PreviewMouseMove += AdornerOnMouseMove;
            cropAdorner.PreviewMouseLeftButtonUp += AdornerOnMouseLeftButtonUp;

            cropTool.Redraw(0, 0, 0, 0);
        }

        public CropArea GetCroppedArea() =>
            new CropArea(
                cropAdorner.RenderSize,
                new Rect(cropTool.TopLeftX, cropTool.TopLeftY, cropTool.Width, cropTool.Height)
            );

        private void AdornerOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvas.ReleaseMouseCapture();
            currentToolState = completeState;
        }

        private void AdornerOnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(canvas);
            var newPosition = currentToolState.OnMouseMove(point);
            if (newPosition.HasValue)
            {
                cropTool.Redraw(newPosition.Value.Left, newPosition.Value.Top, newPosition.Value.Width, newPosition.Value.Height);
            }
        }

        private void AdornerOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            canvas.CaptureMouse();
            var point = e.GetPosition(canvas);
            var touch = GetTouchPoint(point);
            if (touch == TouchPoint.OutsideRectangle)
            {
                if (cropTool.Width >= 1 && cropTool.Height >= 1)
                {
                    // Don't deselect when clicking outside rect
                    return;
                }
                currentToolState = createState;
            }
            else if (touch == TouchPoint.InsideRectangle)
            {
                currentToolState = dragState;
            }
            currentToolState.OnMouseDown(point);
        }

        private TouchPoint GetTouchPoint(Point mousePoint)
        {
            // 13 to not hit resize function

            //left
            if (mousePoint.X < cropTool.TopLeftX + 13)
            {
                return TouchPoint.OutsideRectangle;
            }
            //right
            if (mousePoint.X > cropTool.BottomRightX - 13)
            {
                return TouchPoint.OutsideRectangle;
            }
            //top
            if (mousePoint.Y < cropTool.TopLeftY + 13)
            {
                return TouchPoint.OutsideRectangle;
            }
            //bottom
            if (mousePoint.Y > cropTool.BottomRightY - 13)
            {
                return TouchPoint.OutsideRectangle;
            }

            return TouchPoint.InsideRectangle;
        }
    }
}