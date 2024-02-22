using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace NUHS.Common
{
    public class AnchorAndFaceCamera : MonoBehaviour
    {
        public Vector3 offset;

        [SerializeField] private bool isContinuousAnchor = true, offsetFromCamera = true;
        [SerializeField] private Transform anchor;
        private Transform offsetReference;

        private Transform _camTransform;

        private void Start()
        {
            if (offsetFromCamera)
            {
                offsetReference = _camTransform;
            }
            else 
            {
                offsetReference = anchor;
            }
            Anchor();
        }

        private void Awake()
        {
            _camTransform = CameraCache.Main.transform;
        }

        private void LateUpdate()
        {
            if (isContinuousAnchor)
            {
                Anchor();
            }
            transform.rotation = Quaternion.LookRotation(transform.position - _camTransform.position, _camTransform.up).normalized;
        }

        public void Anchor()
        {
            transform.position = anchor.position + offsetReference.right * offset.x + offsetReference.up * offset.y;
        }
    }
}
