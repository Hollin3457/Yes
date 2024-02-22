using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

namespace NUHS.UltraSound.UI
{
    public class BigUltrasoundController : MonoBehaviour
    {
        public float minZoomValue = 1f;
        public float maxZoomValue = 25f;
        public PinchSlider pinchSlider;
        public TMP_Text sliderText;
        public Image bigImage;

        private void OnEnable()
        {
            pinchSlider.SliderValue = 0;
            UpdateZoom(0);
        }

        // Start is called before the first frame update
        public void OnZoomInButtonPressed()
        {
            pinchSlider.SliderValue += 0.25f;
            pinchSlider.SliderValue = Mathf.Clamp(pinchSlider.SliderValue, 0, 1.0f);
        }

        // Update is called once per frame
        public void OnZoomOutButtonPressed()
        {
            pinchSlider.SliderValue -= 0.25f;
            pinchSlider.SliderValue = Mathf.Clamp(pinchSlider.SliderValue, 0, 1.0f);
        }

        public void OnZoomResetPressed()
        {
            pinchSlider.SliderValue = 0;
        }

        public void OnSliderValueUpdated()
        {
            float zoomLevel = Mathf.Lerp(minZoomValue, maxZoomValue, pinchSlider.SliderValue);
            sliderText.text = ((int)(pinchSlider.SliderValue * 100)).ToString() + "%";
            UpdateZoom(zoomLevel);
        }

        private void UpdateZoom(float zoomLevel)
        {
            bigImage.transform.localScale = new Vector3(zoomLevel, zoomLevel, 1f);
        }
    }

}
