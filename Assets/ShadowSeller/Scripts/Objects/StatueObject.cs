using UnityEngine;

namespace ShadowSeller.Core
{
    // 동상: E키로 플레이어 반대 방향으로 밀기. ShadowZone(A) 항상 활성.
    public class StatueObject : MonoBehaviour, IUsable
    {
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private float pushDistance    = 1.5f;

        public string UseHint => "E 밀기";

        public void OnUse(PlayerController user)
        {
            Vector2 pushDir = ((Vector2)transform.position - (Vector2)user.transform.position).normalized;
            transform.position += (Vector3)(pushDir * pushDistance);
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
            Gizmos.color = new Color(0.7f, 0.7f, 0.9f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
