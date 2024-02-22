using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace NUHS.Common
{
    public static class PlacementUtil
    {
        /// <summary>
        /// Position <paramref name="target"/> relative to <paramref name="relativeObj"/> with given
        /// <paramref name="offset"/>, and face the camera.
        /// </summary>
        public static void PositionAndFaceCamera(Transform target, Transform relativeObj, Vector3 offset)
        {
            var camTransform = CameraCache.Main.transform;
            var relativeObjToCam = (camTransform.position - relativeObj.position).normalized;
            var right = Vector3.Cross(relativeObjToCam, Vector3.up) * offset.x;
            var up = Vector3.up * offset.y;
            target.position = relativeObj.position + right + up;
            target.rotation = Quaternion.LookRotation(target.position - camTransform.position);
        }
    }
}
