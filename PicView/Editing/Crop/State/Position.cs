namespace PicView.Editing.Crop.State
{
    internal readonly struct Position
    {
        public double Left { get; }
        public double Top { get;  }
        public double Width { get; }
        public double Height { get; }

        public Position(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}