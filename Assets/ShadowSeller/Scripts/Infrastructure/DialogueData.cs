using UnityEngine;

namespace ShadowSeller.Core
{
    [CreateAssetMenu(menuName = "ShadowSeller/DialogueData", fileName = "NewDialogue")]
    public class DialogueData : ScriptableObject
    {
        [Tooltip("대화 줄 목록 — 순서대로 출력됨")]
        public DialogueLine[] lines;
    }
}
