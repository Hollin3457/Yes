using UnityEngine;

namespace NUHS.Common
{
    public class HideAfterDurationOrEvent : MonoBehaviour
    {
        [Header("Receiving Events")]
        [SerializeField] private SimpleEvent onTrigger;

        [Header("Config")]
        [Tooltip("Seconds until this object hides. Has no effect if value is less than or equal to 0.")]
        [SerializeField] private float duration;

        private void OnEnable()
        {
            onTrigger?.Register(Hide);
            if (duration > 0)
            {
                Invoke(nameof(Hide), duration);
            }
        }

        private void OnDisable()
        {
            onTrigger?.Unregister(Hide);
            CancelInvoke();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}