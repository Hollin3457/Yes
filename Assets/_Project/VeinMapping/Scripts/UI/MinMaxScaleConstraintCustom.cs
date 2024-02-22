// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Component for setting the min/max scale values for ObjectManipulator
    /// or BoundsControl
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/MinMaxScaleConstraintCustom")]
    public class MinMaxScaleConstraintCustom : TransformConstraint
    {
        #region Properties

        [SerializeField]
        [Tooltip("Minimum scaling allowed")]
        private Vector3 minimumScale = Vector3.one * 0.2f;

        /// <summary>
        /// Accessor for the minimum scale along all three axes.
        /// </summary>
        public Vector3 ScaleMinimumVector => minimumScale;

        [SerializeField]
        [Tooltip("Maximum scaling allowed")]
        private Vector3 maximumScale = Vector3.one * 2f;

        /// <summary>
        /// Accessor for the maximum scale along all three axes.
        /// </summary>
        public Vector3 ScaleMaximumVector => maximumScale;

        public override TransformFlags ConstraintType => TransformFlags.Scale;

        #endregion Properties

        #region Public Methods
        public override void Initialize(MixedRealityTransform worldPose)
        {
            base.Initialize(worldPose);
        }

        /// <summary>
        /// Clamps the transform scale to the scale limits set by
        /// <see cref="minimumScale"/> and <see cref="maximumScale"/>.
        /// </summary>
        public override void ApplyConstraint(ref MixedRealityTransform transform)
        {
            var scale = transform.Scale;
            for (int i = 0; i < 3; ++i)
            {
                if (scale[i] < minimumScale[i])
                {
                    scale[i] = minimumScale[i];
                }
                else if (scale[i] > maximumScale[i])
                {
                    scale[i] = maximumScale[i];
                }
            }

            transform.Scale = scale;
        }

        #endregion Public Methods
    }
}