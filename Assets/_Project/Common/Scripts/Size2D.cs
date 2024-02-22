using UnityEngine;

namespace NUHS.Common
{
    public readonly struct Size2D
    {
        private Size2D(Length x, Length y)
        {
            this.X = x;
            this.Y = y;
        }

        public Length X { get; }
        public Length Y { get; }

        public static Size2D FromMeters(float x, float y) => new Size2D(
            Length.FromMeters(x),
            Length.FromMeters(y));
        public static Size2D FromCM(float x, float y) => new Size2D(
            Length.FromCM(x),
            Length.FromCM(y));
        public static Size2D FromMM(float x, float y) => new Size2D(
            Length.FromMM(x),
            Length.FromMM(y));

        public static Size2D FromMeters(Vector3 size) => new Size2D(
            Length.FromMeters(size.x),
            Length.FromMeters(size.y));
        public static Size2D FromCM(Vector3 size) => new Size2D(
            Length.FromCM(size.x),
            Length.FromCM(size.y));
        public static Size2D FromMM(Vector3 size) => new Size2D(
            Length.FromMM(size.x),
            Length.FromMM(size.y));
    }
}
