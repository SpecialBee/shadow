using UnityEngine;

namespace ShadowSeller.Core
{
    // 옮길 수 있는 물건: F키로 들기/놓기 토글.
    //   - 들고 있는 동안 플레이어 이동 방향에 맞춰 물건 위치가 앞쪽에 붙음
    //   - F 놓기 시 현재 보는 방향 앞에 툭 놓임
    //   - 들고 있는 동안 ShadowProjector(_Shadow GO) 비활성화 → 그림자 사라짐
    //   - weight: 나중에 스태미나 소모량 계산에 사용 예정 (현재 미사용)
    public class BringObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private float holdOffset = 0.7f;  // 플레이어 앞 거리

        // 무게 — 추후 스태미나 소모량 계산에 사용 예정
        // 1 = 가벼움, 2 = 보통, 3 = 무거움 (기준값, 추후 조정)
        [SerializeField] public float weight = 1f;

        public bool IsCarried { get; private set; }

        private Collider2D       _col;
        private PlayerController _carrier;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
        }

        // ── IInteractable ─────────────────────────────────────────────────────

        public void OnPickup(Transform carrier)
        {
            IsCarried = true;
            _carrier  = carrier.GetComponent<PlayerController>();

            if (_col != null) _col.enabled = false;

            var shadowGo = transform.Find("_Shadow");
            if (shadowGo != null) shadowGo.gameObject.SetActive(false);
        }

        public void OnDrop()
        {
            if (_carrier != null)
            {
                Vector2 dropPos = (Vector2)_carrier.transform.position
                                + _carrier.LastMoveDir * holdOffset;
                transform.position = dropPos;
            }

            IsCarried = false;
            _carrier  = null;

            if (_col != null) _col.enabled = true;

            var shadowGo = transform.Find("_Shadow");
            if (shadowGo != null) shadowGo.gameObject.SetActive(true);
        }

        // ── 들고 있는 동안 매 프레임 위치 동기화 ─────────────────────────────

        private void LateUpdate()
        {
            if (!IsCarried || _carrier == null) return;

            Vector2 targetPos = (Vector2)_carrier.transform.position
                              + _carrier.LastMoveDir * holdOffset;
            transform.position = targetPos;
        }

        // ── 범위 감지 ─────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCarried) return;
            other.GetComponent<PlayerInteraction>()?.SetNearby(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsCarried) return;
            other.GetComponent<PlayerInteraction>()?.ClearNearby(this);
        }

        // ── 에디터 Gizmo ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = IsCarried
                ? new Color(0.2f, 0.9f, 0.4f, 0.3f)
                : new Color(0.9f, 0.7f, 0.2f, 0.25f);

            var col = GetComponent<Collider2D>();
            if (col is CircleCollider2D circle)
                Gizmos.DrawWireSphere(transform.position, circle.radius);
            else
                Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
