using System.Collections;
using TMPro;
using UnityEngine;
using NUHS.Common.UI;

namespace NUHS.UltraSound.UI
{
    public class UltrasoundConnectionController : MonoBehaviour
    {
        [Header("UI Dependency")]
        [SerializeField] private TransactionalPrompt IPAddressPrompt;
        [SerializeField] private TMP_InputField IPAddressInputField;
        [SerializeField] private TransactionalPrompt notConnectedPrompt;
        
        [Header("Sending Events")] 
        [SerializeField] private StringEvent onIPAddressConfirmed;
        [SerializeField] private SimpleEvent onIPAddressRetry;

        public void PromptForIPAddress(string defaultIPAddress)
        {
            IPAddressPrompt.gameObject.SetActive(true);
            IPAddressInputField.text = defaultIPAddress;
            IPAddressPrompt.SetupButtonAction(OnIPAddressConfirmed, null);
        }

        public void ShowConnectionFail()
        {
            notConnectedPrompt.gameObject.SetActive(true);
            notConnectedPrompt.SetupButtonAction(()=>onIPAddressRetry.Send(), null);
        }

        public void CloseIPAddressUI()
        {
            IPAddressPrompt.gameObject.SetActive(false);
            notConnectedPrompt.gameObject.SetActive(false);
        }

        private void OnIPAddressConfirmed()
        {
            onIPAddressConfirmed.Send(IPAddressInputField.text);
        }
    }
}