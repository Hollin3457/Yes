namespace NUHS.Common
{
    public readonly struct Length
    {
        private readonly float meters;

        private Length(float meters)
        {
            this.meters = meters;
        }

        public float Meters => meters;
        public float CM => meters * 100;
        public float MM => meters * 1000;

        public static Length FromMeters(float meters) => new Length(meters);
        public static Length FromCM(float cm) => new Length(cm * 0.01f);
        public static Length FromMM(float mm) => new Length(mm * 0.001f);
    }
}
