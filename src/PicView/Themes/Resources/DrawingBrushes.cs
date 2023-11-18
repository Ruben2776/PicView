using PicView.ConfigureSettings;
using System.Windows;
using System.Windows.Media;

namespace PicView.Themes.Resources
{
    internal static class DrawingBrushes
    {
        public static DrawingBrush CheckerboardDrawingBrush(Color color)
        {
            return CheckerboardDrawingBrush(color, ConfigColors.BackgroundBorderColor, 30);
        }

        public static DrawingBrush CheckerboardDrawingBrush(Color color, Color color2, int size)
        {
            var draw = new DrawingBrush
            {
                Viewport = new Rect(0, 0, size, size),
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            var drawingroup = new DrawingGroup();

            var geoBlack = new GeometryDrawing
            {
                Brush = new SolidColorBrush(color)
            };

            var geoGroup1 = new GeometryGroup();
            var rectangleGemetry1 = new RectangleGeometry(new Rect(0, 0, size, size));
            var rectangleGemetry2 = new RectangleGeometry(new Rect(size, size, size, size));
            geoGroup1.Children.Add(rectangleGemetry1);
            geoGroup1.Children.Add(rectangleGemetry2);

            geoBlack.Geometry = geoGroup1;

            var geoWhite = new GeometryDrawing
            {
                Brush = new SolidColorBrush(color2)
            };

            var geoGroup2 = new GeometryGroup();
            var rectangleGemetry3 = new RectangleGeometry(new Rect(5, 0, 5, 5));
            var rectangleGemetry4 = new RectangleGeometry(new Rect(0, 5, 5, 5));
            geoGroup1.Children.Add(rectangleGemetry3);
            geoGroup1.Children.Add(rectangleGemetry4);

            geoWhite.Geometry = geoGroup2;

            drawingroup.Children.Add(geoBlack);
            drawingroup.Children.Add(geoWhite);
            draw.Drawing = drawingroup;

            return draw;
        }
    }
}