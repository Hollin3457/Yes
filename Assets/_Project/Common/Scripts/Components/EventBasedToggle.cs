using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace NUHS.Common
{
    public class EventBasedToggle : MonoBehaviour
    {
        [Header("Sending Events")]
        [SerializeField] private BoolEvent toggleEvent;

        [Header("UI Dependency")]
        [SerializeField] private PressableButton toggleOnButton;
        [SerializeField] private PressableButton toggleOffButton;

        private void Awake()
        {
            toggleOnButton.ButtonReleased.AddListener(OnToggledOn);
            toggleOffButton.ButtonReleased.AddListener(OnToggledOff);
        }

        private void OnToggledOn()
        {
            toggleOnButton.gameObject.SetActive(false);
            toggleOffButton.gameObject.SetActive(true);
            toggleEvent.Send(true);
        }

        private void OnToggledOff()
        {
            toggleOnButton.gameObject.SetActive(true);
            toggleOffButton.gameObject.SetActive(false);
            toggleEvent.Send(false);
        }
    }
}