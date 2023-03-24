using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PicView.Editing.Crop.Tools
{
    internal class CropTool
    {
        private readonly Canvas canvas;
        private readonly CropShape cropShape;
        private readonly ShadeTool shadeService;
        private readonly ThumbTool thumbService;
        private readonly TextTool textService;

        public double TopLeftX => Canvas.GetLeft(cropShape.Shape);
        public double TopLeftY => Canvas.GetTop(cropShape.Shape);
        public double BottomRightX => Canvas.GetLeft(cropShape.Shape) + cropShape.Shape.Width;
        public double BottomRightY => Canvas.GetTop(cropShape.Shape) + cropShape.Shape.Height;
        public double Height => cropShape.Shape.Height;
        public double Width => cropShape.Shape.Width;

        public CropTool(Canvas canvas)
        {
            this.canvas = canvas;
            cropShape = new CropShape(new Rectangle
            {
                Height = 4,
                Width = 4,
                Stroke = (SolidColorBrush)Application.Current.Resources["MainColorBrush"],
                StrokeThickness = 2
            }, canvas);

            shadeService = new ShadeTool(canvas, this);
            thumbService = new ThumbTool(canvas, this);
            textService = new TextTool(this);

            this.canvas.Children.Add(cropShape.Shape);

            this.canvas.Children.Add(shadeService.ShadeOverlay);

            this.canvas.Children.Add(thumbService.BottomMiddle);
            this.canvas.Children.Add(thumbService.LeftMiddle);
            this.canvas.Children.Add(thumbService.TopMiddle);
            this.canvas.Children.Add(thumbService.RightMiddle);
            this.canvas.Children.Add(thumbService.TopLeft);
            this.canvas.Children.Add(thumbService.TopRight);
            this.canvas.Children.Add(thumbService.BottomLeft);
            this.canvas.Children.Add(thumbService.BottomRight);

            this.canvas.Children.Add(textService.Border);
        }

        public void Redraw(double newX, double newY, double newWidth, double newHeight)
        {
            cropShape.Redraw(newX, newY, newWidth, newHeight);
            shadeService.Redraw();
            thumbService.Redraw();
            textService.Redraw();
        }
    }
}