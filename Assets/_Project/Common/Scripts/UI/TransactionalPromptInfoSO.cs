using UnityEngine;

namespace NUHS.Common.UI
{
    [CreateAssetMenu(fileName = "TransactionalPromptInfoSO", menuName = "UI/Transactional Prompt Info SO")]
    public class TransactionalPromptInfoSO : ScriptableObject
    {
        [TextArea] 
        public string promptTitle;
        [TextArea] 
        public string promptContent;

        public bool hasInputField;
        public bool cancelable;
    }
}