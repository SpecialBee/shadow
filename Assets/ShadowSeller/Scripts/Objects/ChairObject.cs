using UnityEngine;

namespace ShadowSeller.Core
{
    // 의자: E키로 플레이어 쪽으로 끌어당김. ShadowZone(C) 항상 활성.
    public class ChairObject : MonoBehaviour, IUsable
    {
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private float pullDistance    = 1.5f;

        public string UseHint => "E 끌기";

        public void OnUse(PlayerController user)
        {
            Vector2 toPlayer = ((Vector2)user.transform.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(toPlayer * pullDistance);
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
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
