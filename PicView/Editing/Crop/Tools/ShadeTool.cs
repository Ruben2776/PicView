using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PicView.Editing.Crop.Tools
{
    internal class ShadeTool
    {
        private readonly CropTool _cropTool;
        private readonly RectangleGeometry _rectangleGeo;

        public Path ShadeOverlay { get; set; }

        public ShadeTool(Canvas canvas, CropTool cropTool)
        {
            _cropTool = cropTool;

            ShadeOverlay = new Path
            {
                Fill = Brushes.Black,
                Opacity = 0.4
            };

            var geometryGroup = new GeometryGroup();
            RectangleGeometry geometry1 =
                new RectangleGeometry(new Rect(new Size(canvas.Width, canvas.Height)));
            _rectangleGeo = new RectangleGeometry(
                new Rect(
                    _cropTool.TopLeftX,
                    _cropTool.TopLeftY,
                    _cropTool.Width,
                    _cropTool.Height
                )
    );

            geometryGroup.Children.Add(geometry1);
            geometryGroup.Children.Add(_rectangleGeo);
            ShadeOverlay.Data = geometryGroup;
        }

        public void Redraw()
        {
            _rectangleGeo.Rect = new Rect(
                _cropTool.TopLeftX,
                _cropTool.TopLeftY,
                _cropTool.Width,
                _cropTool.Height
            );
        }
    }
}