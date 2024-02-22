using UnityEngine;

namespace NUHS.Common.UI
{
    [CreateAssetMenu(fileName = "PassivePromptInfoSO", menuName = "UI/Passive Prompt Info SO")]
    public class PassivePromptInfoSO : ScriptableObject
    {
        public Sprite promptSprite;
        [TextArea] 
        public string promptText;
    }
}