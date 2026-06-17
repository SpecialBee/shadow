using UnityEngine;

namespace ShadowSeller.Core
{
    [CreateAssetMenu(menuName = "ShadowSell/Quest Data", fileName = "QuestData")]
    public class QuestData : ScriptableObject
    {
        [Tooltip("고유 ID. 다른 퀘스트와 겹치지 않아야 함.")]
        public string questId;
        [Tooltip("UI에 표시할 퀘스트 이름.")]
        public string questName;
        [Min(1), Tooltip("완료에 필요한 진행 횟수 (1이면 단순 완료).")]
        public int totalCount = 1;
    }
}
