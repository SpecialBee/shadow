using UnityEngine;

namespace ShadowSeller.Core
{
    // 문: E키로 열기/닫기 토글
    // 씬 구성:
    //   Door (Square 스프라이트, Tag: "Door")
    //   ├── SpriteRenderer + Box Collider 2D (isTrigger=false)
    //   └── DoorTrigger (빈 오브젝트)
    //       ├── Circle Collider 2D (isTrigger=true)
    //       └── DoorObject.cs
    //
    // Inspector에서 doorCollider, doorRenderer, openSprite, closedSprite 연결할 것
    public class DoorObject : MonoBehaviour, IUsable
    {
        [SerializeField] private Collider2D     doorCollider;   // Door 본체 Box Collider 2D
        [SerializeField] private SpriteRenderer doorRenderer;   // Door 본체 SpriteRenderer
        [SerializeField] private Sprite         closedSprite;   // 닫힌 문 스프라이트
        [SerializeField] private Sprite         openSprite;     // 열린 문 스프라이트
        [SerializeField] private bool           startOpen = false;

        private bool _isOpen;

        public string UseHint => _isOpen ? "E 문닫기" : "E 문열기";

        private void Awake()
        {
            _isOpen = startOpen;
            ApplyState();
        }

        public void OnUse(PlayerController user)
        {
            _isOpen = !_isOpen;
            ApplyState();
        }

        private void ApplyState()
        {
            if (doorCollider != null)
                doorCollider.enabled = !_isOpen;

            if (doorRenderer != null)
            {
                // 스프라이트가 지정되어 있으면 스위칭, 없으면 켜고 끄기
                if (openSprite != null && closedSprite != null)
                {
                    doorRenderer.enabled = true;
                    doorRenderer.sprite  = _isOpen ? openSprite : closedSprite;
                }
                else
                {
                    doorRenderer.enabled = !_isOpen;
                }
            }
        }

        // ── 범위 감지 ─────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.SetNearbyUsable(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.ClearNearbyUsable(this);
        }

        // ── 에디터 Gizmo ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = _isOpen
                ? new Color(0.2f, 0.8f, 0.3f, 0.25f)
                : new Color(0.8f, 0.2f, 0.2f, 0.25f);

            var col = GetComponent<Collider2D>();
            if (col is CircleCollider2D circle)
                Gizmos.DrawWireSphere(transform.position, circle.radius);
            else
                Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}
