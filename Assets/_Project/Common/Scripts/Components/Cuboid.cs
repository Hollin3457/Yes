using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;
using UnityEngine;

namespace NUHS.Common
{
    public sealed class Cuboid : MonoBehaviour
    {
        [Header("Sending Events")]
        [SerializeField] private SimpleEvent onManipulationStarted;

        [Header("Configuration")]
        [SerializeField] private HandleScaleMode scaleMode;

        private BoundsControl _boundsControl;
        private ObjectManipulator _objectManipulator;
        private Vector3 _defaultScale;
        
        public Size3D Size => Size3D.FromMeters(transform.localScale.x, transform.localScale.y, transform.localScale.z);

        public void Init()
        {
            _boundsControl = GetComponent<BoundsControl>();
            _objectManipulator = GetComponent<ObjectManipulator>();
            _defaultScale = transform.localScale;
            _boundsControl.ScaleHandlesConfig.ScaleBehavior = scaleMode;
        }

        private void Awake()
        {
            Init();
            _boundsControl.ScaleStarted.AddListener(() => onManipulationStarted.Send());
            _boundsControl.RotateStarted.AddListener(() => onManipulationStarted.Send());
            _boundsControl.TranslateStarted.AddListener(() => onManipulationStarted.Send());
            _objectManipulator.OnManipulationStarted.AddListener(OnManipulationStarted);
        }

        public void ResetScale()
        {
            transform.localScale = _defaultScale;
        }

        public void EnableManipulation(bool enabled)
        {
            if (_boundsControl == null)
            {
                return;
            }

            _boundsControl.enabled = enabled;
            _objectManipulator.enabled = enabled;
        }

        public void SetScaleMode(HandleScaleMode scaleMode)
        {
            if (_boundsControl == null)
            {
                return;
            }

            _boundsControl.ScaleHandlesConfig.ScaleBehavior = scaleMode;
        }

        private void OnManipulationStarted(ManipulationEventData _)
        {
            onManipulationStarted.Send();
        }
    }
}
