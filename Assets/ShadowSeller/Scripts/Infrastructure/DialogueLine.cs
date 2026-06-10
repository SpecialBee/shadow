using UnityEngine;

namespace ShadowSeller.Core
{
    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("말하는 캐릭터 이름")]
        public string speakerName;

        [TextArea(2, 5)]
        [Tooltip("대화 내용")]
        public string text;
    }
}
