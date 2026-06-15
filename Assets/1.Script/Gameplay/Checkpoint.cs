using UnityEngine;

namespace ShadowSeller.Core
{
    // 체크포인트 — 플레이어가 트리거에 진입하면 현재 상태를 저장.
    // CircleCollider2D(isTrigger=true)를 자동 생성.
    // 이미 활성화된 체크포인트는 재저장하지 않음.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("감지 반경 (월드 단위)")]
        [SerializeField] private float radius = 1.5f;

        [Header("색상")]
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 1f, 0.4f);
        [SerializeField] private Color activeColor   = new Color(0.3f, 1f, 0.4f, 0.7f);

        private SpriteRenderer   _sr;
        private CircleCollider2D _col;
        private bool             _activated;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.color = inactiveColor;

            _col           = gameObject.AddComponent<CircleCollider2D>();
            _col.isTrigger = true;
            _col.radius    = LocalRadius();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated) return;
            if (!other.CompareTag("Player")) return;

            _activated = true;
            _sr.color  = activeColor;

            CheckpointManager.Instance?.SaveCheckpoint(other.transform.position);
            Debug.Log($"[Checkpoint] '{name}' 활성화");
        }

        private float LocalRadius()
        {
            float scale = Mathf.Abs(transform.lossyScale.x);
            return scale > 0.001f ? radius / scale : radius;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _activated ? activeColor : inactiveColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var col = GetComponent<CircleCollider2D>();
            if (col != null && col.isTrigger) col.radius = LocalRadius();
        }
#endif
    }
}
