using UnityEngine;

namespace NUHS.UltraSound.Profile
{
    public class UltrasoundProfile
    {
        public bool IsSummary { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Texture2D Image { get; set; }
        public bool IsHidden { get; set; }
        public Vector3 DeviceSizeInCm { get; set; }
        public string DeviceType { get; set; }
        public string DeviceConfig { get; set; }
    }
}
