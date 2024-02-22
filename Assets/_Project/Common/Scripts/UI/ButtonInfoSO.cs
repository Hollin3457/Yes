using UnityEngine;

namespace NUHS.Common.UI
{
    [CreateAssetMenu(fileName = "ButtonInfoSO", menuName = "UI/Button Info SO")]
    public class ButtonInfoSO : ScriptableObject
    {
        public Sprite buttonSprite;
        public string buttonText;
    }
}