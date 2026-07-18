using System;

namespace Patches.Domain
{
    [Serializable]
    public struct PatchBounds : IEquatable<PatchBounds>
    {
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;

        public PatchBounds(int minX, int maxX, int minY, int maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public int Width => (MaxX - MinX) + 1;
        public int Height => (MaxY - MinY) + 1;
        public int Area => Width * Height;

        public bool Contains(int x, int y)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
        }

        public bool Overlaps(PatchBounds other)
        {
            return MinX <= other.MaxX && MaxX >= other.MinX &&
                   MinY <= other.MaxY && MaxY >= other.MinY;
        }

        public bool Equals(PatchBounds other)
        {
            return MinX == other.MinX && MaxX == other.MaxX && MinY == other.MinY && MaxY == other.MaxY;
        }

        public override bool Equals(object obj)
        {
            return obj is PatchBounds other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + MinX;
                hash = hash * 23 + MaxX;
                hash = hash * 23 + MinY;
                hash = hash * 23 + MaxY;
                return hash;
            }
        }

        public static bool operator ==(PatchBounds left, PatchBounds right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PatchBounds left, PatchBounds right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({MinX},{MinY}) to ({MaxX},{MaxY}) [Size: {Width}x{Height}, Area: {Area}]";
        }
    }
}
