using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using UnityEngine;

namespace NUHS.Common.UI
{
    public class CommonButton : MonoBehaviour
    {
        private float BUTTON_RIGHT_INIT_VALUE = 0f;
        private float BUTTON_SCALE = 0.04f;
        private float BOUND_CENTER_INIT_VALUE = -0.03f;
        private float BOUND_SIZE_INIT_VALUE = 0.04f;

        [Header("Button Setting")]
        [SerializeField] private ButtonInfoSO buttonInfoSo;
        [SerializeField] private bool isEmphasis;
        
        [Header("Button Info Dependency")]
        [SerializeField] private SpriteRenderer buttonSprite;
        [SerializeField] private TMP_Text buttonText;
        
        [Header("Button Resizing Dependency")]
        [SerializeField] private Transform buttonRightTop;
        [SerializeField] private Transform buttonRightBottom;
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private NearInteractionTouchable nearInteractionTouchable;
        
        [Header("Button Rendering Dependency")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color textColor;
        [SerializeField] private Color emphasisTextColor;
        [SerializeField] private Material[] pressedMats;
        [SerializeField] private Material[] notPressedMats;
        [SerializeField] private Material[] emphasisPressedMats;
        [SerializeField] private Material[] emphasisNotPressedMats;
        private Action _buttonAction;
        private WaitForSeconds _buttonActionTriggerDelay;
        private bool _isPressed;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            SetButtonInfo();
            SetEmphasis(isEmphasis);
        }
#endif
        private void Awake()
        {
            SetButtonInfo();
            SetEmphasis(isEmphasis);
            _buttonActionTriggerDelay = new WaitForSeconds(0.3f);
        }
        
        public void OnButtonPressed()
        {
            buttonRenderer.materials = isEmphasis ? emphasisPressedMats : pressedMats;
            if (_isPressed) return;
            StartCoroutine(CoTriggerButton());
        }

        private IEnumerator CoTriggerButton()
        {
            _isPressed = true;
            yield return _buttonActionTriggerDelay;
            _buttonAction?.Invoke();
            _isPressed = false;
        }

        public void OnButtonReleased()
        {
            buttonRenderer.materials = isEmphasis ? emphasisNotPressedMats : notPressedMats;
        }

        public void AddButtonAction(Action action)
        {
            _buttonAction += action;
        }

        public void RemoveAllButtonAction()
        {
            _buttonAction = null;
        }

        private void SetButtonInfo()
        {
            buttonSprite.sprite = buttonInfoSo.buttonSprite;
            buttonText.SetText(buttonInfoSo.buttonText);
            var textLength = buttonText.preferredWidth;
            
            //scale button
            var newButtonRightValue = BUTTON_RIGHT_INIT_VALUE - textLength/BUTTON_SCALE;
            var tp = buttonRightTop.localPosition;
            buttonRightTop.localPosition = new Vector3(newButtonRightValue, tp.y, tp.z);
            var bp = buttonRightBottom.localPosition;
            buttonRightBottom.localPosition = new Vector3(newButtonRightValue, bp.y, bp.z);
            
            //scale collider
            var bc = boxCollider.center;
            var newBoxCenterXValue = BOUND_CENTER_INIT_VALUE + textLength/2f;
            boxCollider.center = new Vector3(newBoxCenterXValue, bc.y, bc.z);
            var bs = boxCollider.size;
            var newBoxSizeXValue = BOUND_SIZE_INIT_VALUE + textLength;
            boxCollider.size = new Vector3(newBoxSizeXValue, bs.y, bs.z);

            //scale interactable bound
            var nc = nearInteractionTouchable.LocalCenter;
            nearInteractionTouchable.SetLocalCenter(new Vector3(newBoxCenterXValue, nc.y, nc.z));
            var ns = nearInteractionTouchable.Bounds;
            nearInteractionTouchable.SetBounds(new Vector2(newBoxSizeXValue, ns.y));
        }

        public void SetEmphasis(bool emphasis)
        {
            isEmphasis = emphasis;
            buttonText.color = isEmphasis ? emphasisTextColor : textColor;
            buttonRenderer.materials = isEmphasis ? emphasisNotPressedMats : notPressedMats;
        }
    }
}
