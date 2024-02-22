using NUHS.UltraSound.Profile;
using UnityEngine;

namespace NUHS.UltraSound.ImageReceiving
{
    public class SidecarUltrasoundConfig
    {
        public uint DeviceId;
        public uint CaptureW;
        public uint CaptureH;
        public uint CropX;
        public uint CropY;
        public uint CropW;
        public uint CropH;
        public uint PixelsPerCm;
        public bool DetectPixelsPerCm;
        public uint DetectPixelsPerCmMethod;
        public uint FPS;
        public uint Quality;

        public static SidecarUltrasoundConfig FromProfile(UltrasoundProfile profile)
        {
            return JsonUtility.FromJson<SidecarUltrasoundConfig>(profile.DeviceConfig);
        }
    }
}
