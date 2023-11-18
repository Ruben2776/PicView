using System.Windows;

namespace PicView.Editing.Crop.State
{
    internal interface IToolState
    {
        void OnMouseDown(Point point);

        Position? OnMouseMove(Point point);

        void OnMouseUp(Point point);
    }
}