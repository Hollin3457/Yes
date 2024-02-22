using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NUHS.Common.UI
{
    public class PassivePrompt : MonoBehaviour
    {
        [Header("Setting")]
        [SerializeField] private PassivePromptInfoSO passivePromptInfoSo;
        
        [Header("Dependency")]
        [SerializeField] private TMP_Text promptDescription;
        [SerializeField] private Image promptImage;

        private void OnValidate()
        {
            SetPromptInfo();
        }
        
        private void Awake()
        {
            SetPromptInfo();
        }

        public void SetPromptInfo(PassivePromptInfoSO infoSo)
        {
            passivePromptInfoSo = infoSo;
            SetPromptInfo();
        }

        private void SetPromptInfo()
        {
            if (passivePromptInfoSo == null) return;
            
            promptDescription.SetText(passivePromptInfoSo.promptText);

            if (passivePromptInfoSo.promptSprite != null)
            {
                promptImage.gameObject.SetActive(true);
                promptImage.sprite = passivePromptInfoSo.promptSprite;
            }
            else
            {
                promptImage.gameObject.SetActive(false);
            }
        }
    }
}