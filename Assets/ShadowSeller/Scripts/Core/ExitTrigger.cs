using UnityEngine;

namespace ShadowSeller.Core
{
    // 탈출 지점: 목표 완료 후 플레이어 진입 시 승리
    [RequireComponent(typeof(Collider2D))]
    public class ExitTrigger : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            ObjectiveManager.Instance?.TriggerVictory();
        }

        private void OnDrawGizmos()
        {
            bool ready = ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsComplete;
            Gizmos.color = ready
                ? new Color(0.1f, 1f, 0.3f, 0.45f)
                : new Color(0.5f, 0.5f, 0.5f, 0.3f);

            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
                Gizmos.DrawCube(transform.position, box.size);
            else
                Gizmos.DrawCube(transform.position, Vector3.one * 2f);
        }
    }
}
