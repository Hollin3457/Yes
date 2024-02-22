using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace NUHS.Common
{
    /// <summary>
    /// Component for limiting the axes that can be scaled.
    /// or BoundsControl
    /// </summary>
    public class ScaleAxisConstraint : TransformConstraint
    {
        [SerializeField] private bool xAxis;
        [SerializeField] private bool yAxis;
        [SerializeField] private bool zAxis;

        private Vector3 _startingScale;

        public override TransformFlags ConstraintType => TransformFlags.Scale;

        public override void Initialize(MixedRealityTransform worldPose)
        {
            base.Initialize(worldPose);
            SetScaleLimits();
        }
        public override void ApplyConstraint(ref MixedRealityTransform transform)
        {
            var scale = transform.Scale;
            if (xAxis)
            {
                scale.x = _startingScale.x;
            }
            if (yAxis)
            {
                scale.y = _startingScale.y;
            }
            if (zAxis)
            {
                scale.z = _startingScale.z;
            }
            transform.Scale = scale;
        }

        private void SetScaleLimits()
        {
            _startingScale = transform.localScale;
        }
    }
}