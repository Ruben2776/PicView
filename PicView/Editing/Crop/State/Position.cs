using System;

namespace PicView.Editing.Crop.State
{
    internal readonly struct Position : IEquatable<Position>
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

        public override bool Equals(object obj)
        {
            return obj is Position position && Equals(position);
        }

        public bool Equals(Position other)
        {
            return Left == other.Left &&
                   Top == other.Top &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}