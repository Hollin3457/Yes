using System;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace NUHS.Common.UI
{
    public class TransactionalPrompt : MonoBehaviour
    {
        [Header("Setting")]
        [SerializeField] private TransactionalPromptInfoSO transactionalPromptInfoSo;
        
        [Header("Dependency")]
        [SerializeField] private TMP_Text promptTitle;
        [SerializeField] private TMP_Text promptContent;
        [SerializeField] private GameObject inputField;
        [SerializeField] private GameObject cancelButtonHolder;
        [SerializeField] private CommonButton confirmButton;
        [SerializeField] private CommonButton cancelButton;

        private void OnValidate()
        {
            SetPromptInfo();
        }
        
        private void Awake()
        {
            SetPromptInfo();
            confirmButton.AddButtonAction(CloseSelf);
            cancelButton.AddButtonAction(CloseSelf);
        }

        public void SetupButtonAction(Action onConfirmAction, Action onCancelAction)
        {
            confirmButton.RemoveAllButtonAction();
            confirmButton.AddButtonAction(CloseSelf);
            confirmButton.AddButtonAction(onConfirmAction);

            cancelButton.RemoveAllButtonAction();
            cancelButton.AddButtonAction(CloseSelf);
            cancelButton.AddButtonAction(onCancelAction);
        }

        public void SetContentText(string contentMessage)
        {
            transactionalPromptInfoSo.promptContent = contentMessage;
            if (string.IsNullOrEmpty(contentMessage))
            {
                promptContent.gameObject.SetActive(false);
            }
            else
            {
                promptContent.gameObject.SetActive(true);
                promptContent.SetText(contentMessage);
            }
        }

        private void SetPromptInfo()
        {
            if (transactionalPromptInfoSo == null) return;
            
            promptTitle.SetText(transactionalPromptInfoSo.promptTitle);
            if (string.IsNullOrEmpty(transactionalPromptInfoSo.promptContent))
            {
                promptContent.gameObject.SetActive(false);
            }
            else
            {
                promptContent.gameObject.SetActive(true);
                promptContent.SetText(transactionalPromptInfoSo.promptContent);
            }
            inputField.SetActive(transactionalPromptInfoSo.hasInputField);
            cancelButtonHolder.SetActive(transactionalPromptInfoSo.cancelable);
        }

        private void CloseSelf()
        {
            gameObject.SetActive(false);
        }
    }
}