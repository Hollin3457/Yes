using UnityEngine;

namespace NUHS.Common
{
    public readonly struct Size3D
    {
        private Size3D(Length x, Length y, Length z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Length X { get; }
        public Length Y { get; }
        public Length Z { get; }

        public static Size3D FromMeters(float x, float y, float z) => new Size3D(
            Length.FromMeters(x),
            Length.FromMeters(y),
            Length.FromMeters(z));
        public static Size3D FromCM(float x, float y, float z) => new Size3D(
            Length.FromCM(x),
            Length.FromCM(y),
            Length.FromCM(z));
        public static Size3D FromMM(float x, float y, float z) => new Size3D(
            Length.FromMM(x),
            Length.FromMM(y),
            Length.FromMM(z));

        public static Size3D FromMeters(Vector3 size) => new Size3D(
            Length.FromMeters(size.x),
            Length.FromMeters(size.y),
            Length.FromMeters(size.z));
        public static Size3D FromCM(Vector3 size) => new Size3D(
            Length.FromCM(size.x),
            Length.FromCM(size.y),
            Length.FromCM(size.z));
        public static Size3D FromMM(Vector3 size) => new Size3D(
            Length.FromMM(size.x),
            Length.FromMM(size.y),
            Length.FromMM(size.z));
    }
}
