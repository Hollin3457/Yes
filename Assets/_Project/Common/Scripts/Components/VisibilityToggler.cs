using UnityEngine;

namespace NUHS.Common
{
    public class VisibilityToggler : MonoBehaviour
    {
        [Header("Receiving Events")]
        [SerializeField] private BoolEvent onTrigger;

        private void OnEnable()
        {
            onTrigger?.Register(ToggleVisibility);
        }

        private void OnDestroy()
        {
            onTrigger?.Unregister(ToggleVisibility);
        }

        private void ToggleVisibility(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}
