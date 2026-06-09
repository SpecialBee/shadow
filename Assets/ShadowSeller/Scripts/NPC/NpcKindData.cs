using UnityEngine;

namespace ShadowSeller.Core
{
    // NPC 종류별 설정값 ScriptableObject.
    // 시야각/범위, 의심도 상승·감소 속도, 임계값, 이동 속도, 각종 타이머를 인스펙터에서 조정.
    // NPCController가 런타임에 이 데이터를 참조함. 종류마다 별도 에셋을 만들어 할당.
    [CreateAssetMenu(menuName = "ShadowSell/NPC Kind Data", fileName = "NpcKindData_Guard")]
    public class NpcKindData : ScriptableObject
    {
        public NpcKind kind = NpcKind.Guard;

        [Header("Vision")]
        public float viewAngle = 90f;
        public float viewRange = 6f;

        [Header("Individual Suspicion")]
        public float suspicionGainRate   = 30f;
        public float suspicionDecayRate  = 8f;
        public float alertThreshold      = 40f;
        public float chaseThreshold      = 75f;

        [Header("Movement")]
        public float patrolSpeed = 2f;
        public float chaseSpeed  = 4f;

        [Header("Timers")]
        public float arrestTime     = 3f;
        public float sightLoseDelay = 2f;
        public float searchDuration = 5f;
        public float closeRange     = 2f;
    }
}
