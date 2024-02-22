using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private float maxValue;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Slider slider;

    public void Show(float max)
    {
        maxValue = max;
        Show();
    }

    public void Show()
    {
        valueText.text = "0";
        slider.value = 0f;
        slider.gameObject.SetActive(true);
    }

    public void Hide()
    {
        slider.gameObject.SetActive(false);
    }

    public void SetValue(float value)
    {
        slider.value = value / maxValue;
        valueText.text = Mathf.CeilToInt(value < maxValue ? value : maxValue).ToString();
    }

    public float GetValue()
    {
        return slider.value;
    }
}
