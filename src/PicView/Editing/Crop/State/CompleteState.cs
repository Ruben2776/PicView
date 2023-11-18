using System.Windows;

namespace PicView.Editing.Crop.State
{
    internal class CompleteState : IToolState
    {
        public void OnMouseDown(Point point)
        {
            // Blank override
            throw new NotImplementedException();
        }

        public Position? OnMouseMove(Point point)
        {
            return null;
        }

        public void OnMouseUp(Point point)
        {
            // Blank override
            throw new NotImplementedException();
        }
    }
}