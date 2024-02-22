using System.Collections.Generic;
using OpenCVForUnity.CoreModule;

namespace NUHS.UltraSound.Tracking
{
    public class MarkerData
    {
        public float ScaleFactor;
        public Mat Id;
        public List<Mat> Data; 
    }
}
