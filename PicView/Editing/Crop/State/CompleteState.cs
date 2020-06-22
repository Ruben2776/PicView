using System.Windows;

namespace PicView.Editing.Crop.State
{
    internal class CompleteState : IToolState
    {
        /// <summary>
        /// Blank override
        /// </summary>
        /// <param name="point"></param>
        public void OnMouseDown(Point point)
        {
        }

        public Position? OnMouseMove(Point point)
        {
            return null;
        }

        /// <summary>
        /// Blank override
        /// </summary>
        /// <param name="point"></param>
        public void OnMouseUp(Point point)
        {
        }
    }
}
