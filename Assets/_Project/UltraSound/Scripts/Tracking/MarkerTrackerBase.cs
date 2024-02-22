using UnityEngine;

namespace NUHS.UltraSound.Tracking
{
    public abstract class MarkerTrackerBase : MonoBehaviour
    {
        public abstract Vector3 GetImagePos();
        public abstract Vector3 GetImageRot();
        public abstract Vector3 GetMarkerPos();
        public abstract Quaternion GetMarkerRot();
        
        public abstract bool IsMarkerDetected();
        public abstract void StartMarkerDetect();
        public abstract void StopMarkerDetect();
        public abstract void SetOffset(Vector3 pos, Vector3 rot);
    }
}
