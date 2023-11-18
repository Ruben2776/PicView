using PicView.Editing.Crop.Tools;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Editing.Crop.State
{
    internal class CreateState : IToolState
    {
        private readonly CropTool cropTool;
        private readonly Canvas canvas;

        private Point _startPoint;

        public CreateState(CropTool cropTool, Canvas canvas)
        {
            this.cropTool = cropTool;
            this.canvas = canvas;
        }

        public void OnMouseDown(Point point)
        {
            _startPoint = point;
        }

        public Position? OnMouseMove(Point point)
        {
            var left = Math.Min(point.X, _startPoint.X);
            var top = Math.Min(point.Y, _startPoint.Y);
            var width = Math.Abs(point.X - _startPoint.X);
            var height = Math.Abs(point.Y - _startPoint.Y);

            SetCreateBorderLimit(ref left, ref top, ref width, ref height);

            return new Position(left, top, width, height);
        }

        public void OnMouseUp(Point point)
        {
            // Blank override
            throw new NotImplementedException();
        }

        private void SetCreateBorderLimit(ref double newX, ref double newY, ref double newWidth, ref double newHeight)
        {
            //  set drawing limits(canvas borders)
            //  so we can't select area outside of canvas border
            //set top limit
            if (newY < 0)
            {
                newY = 0;
                newHeight = cropTool.Height;
            }

            //set right limit
            if (newX + newWidth > canvas.ActualWidth)
            {
                newWidth = canvas.ActualWidth - newX;
            }

            //set left limit
            if (newX < 0)
            {
                newX = 0;
                newWidth = cropTool.Width;
            }

            //set bottom limit
            if (newY + newHeight > canvas.ActualHeight)
            {
                newHeight = canvas.ActualHeight - newY;
            }
        }
    }
}