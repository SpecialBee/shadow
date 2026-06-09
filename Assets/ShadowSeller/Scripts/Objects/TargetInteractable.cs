using UnityEngine;

namespace ShadowSeller.Core
{
    // 목표 오브젝트: E키 대화 → 목표 완료
    public class TargetInteractable : MonoBehaviour, IUsable
    {
        [SerializeField] private float detectionRadius = 2f;

        public string UseHint => ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsComplete
            ? "E 완료됨"
            : "E 대화";

        public void OnUse(PlayerController user)
        {
            ObjectiveManager.Instance?.Complete();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.SetNearbyUsable(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.ClearNearbyUsable(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
