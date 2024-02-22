using UnityEngine;

namespace NUHS.Common
{
    public class AnchorToTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform anchor;
        [SerializeField] private Vector3 offset;

        private void LateUpdate()
        {
            transform.rotation = target.rotation;
            transform.position = anchor.position + target.right * offset.x + target.up * offset.y;
        }
    }
}
