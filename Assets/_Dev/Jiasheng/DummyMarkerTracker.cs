using UnityEngine;

namespace NUHS.UltraSound.Tracking
{
    public class DummyMarkerTracker : MarkerTrackerBase
    {
        [SerializeField] private Vector3 dummyPosition;
        [SerializeField] private MarkerConfig markerConfig;
        public override Vector3 GetImagePos()
        {
            return dummyPosition;
        }

        public override Vector3 GetImageRot()
        {
            return Vector3.one;
        }

        public override Vector3 GetMarkerPos()
        {
            throw new System.NotImplementedException();
        }

        public override Quaternion GetMarkerRot()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsMarkerDetected()
        {
            throw new System.NotImplementedException();
        }

        public override void StartMarkerDetect()
        {
            throw new System.NotImplementedException();
        }

        public override void StopMarkerDetect()
        {
            throw new System.NotImplementedException();
        }

        public override void SetOffset(Vector3 pos, Vector3 rot)
        {
            markerConfig.posOffset = pos;
            markerConfig.rotationOffset = rot;
            markerConfig.Save();
        }
    }
}