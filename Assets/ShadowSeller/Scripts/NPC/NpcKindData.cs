using UnityEngine;

namespace ShadowSeller.Core
{
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
