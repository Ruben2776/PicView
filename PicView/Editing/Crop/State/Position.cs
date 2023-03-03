using System.Diagnostics.CodeAnalysis;

namespace PicView.Editing.Crop.State
{
    internal readonly struct Position : IEquatable<Position>
    {
        public double Left { get; }
        public double Top { get; }
        public double Width { get; }
        public double Height { get; }

        public Position(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        #region IEquatable<T>
        public override bool Equals(object? obj) => obj != null && obj is Position size && Equals(size);

        public static bool operator ==(Position e1, Position e2)
        {
            return e1.Equals(e2);
        }

        public static bool operator !=(Position e1, Position e2)
        {
            return !(e1 == e2);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public bool Equals([AllowNull] Position other)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}