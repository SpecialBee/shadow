using UnityEngine;

namespace ShadowSeller.Core
{
    // ┌─────────────────────────────────────────────────────────────────────┐
    // │ LightSource                                                         │
    // │                                                                     │
    // │ 역할 1. 플레이어 Lit 판정 (벽 차단 고려)                            │
    // │   CircleCollider2D(trigger) 범위 안에서도 벽 Raycast로 막히면       │
    // │   Lit 판정하지 않음. OnTriggerStay2D로 매 프레임 재확인.            │
    // │                                                                     │
    // │ 역할 2. 그림자 생성 기준                                            │
    // │   ShadowProjector가 Range / WallLayer를 참조.                      │
    // │   오브젝트와 광원 사이에 벽이 있으면 그림자 생성 안 함.             │
    // │                                                                     │
    // │ Light2D는 별도로 수동 조절. 이 스크립트가 건드리지 않음.           │
    // └─────────────────────────────────────────────────────────────────────┘
    [RequireComponent(typeof(CircleCollider2D))]
    public class LightSource : MonoBehaviour
    {
        [SerializeField] private float     range     = 5f;
        [SerializeField] private LayerMask wallLayer;

        public float     Range     => range;
        public LayerMask WallLayer => wallLayer;

        // 현재 Lit 상태 추적 (벽 통과 감지용)
        private PlayerExposureTracker _trackerInRange;
        private bool _isLit;

        // 플레이어 진입 — 즉시 벽 체크 후 Lit 판정
        private void OnTriggerEnter2D(Collider2D other)
        {
            var tracker = other.GetComponent<PlayerExposureTracker>();
            if (tracker == null) return;
            _trackerInRange = tracker;

            if (!WallBlocks(other.transform.position))
            {
                tracker.OnEnterDangerZone();
                _isLit = true;
            }
        }

        // 범위 내 체류 중 — 벽 통과 여부를 매 물리 프레임 재확인
        private void OnTriggerStay2D(Collider2D other)
        {
            if (_trackerInRange == null) return;

            bool blocked = WallBlocks(other.transform.position);

            if (!blocked && !_isLit)
            {
                _trackerInRange.OnEnterDangerZone();
                _isLit = true;
            }
            else if (blocked && _isLit)
            {
                _trackerInRange.OnExitDangerZone();
                _isLit = false;
            }
        }

        // 플레이어 이탈 — Lit 해제
        private void OnTriggerExit2D(Collider2D other)
        {
            var tracker = other.GetComponent<PlayerExposureTracker>();
            if (tracker == null) return;

            if (_isLit)
            {
                tracker.OnExitDangerZone();
                _isLit = false;
            }
            _trackerInRange = null;
        }

        // 광원 → 대상 사이에 벽이 있는지 Raycast로 확인
        public bool WallBlocks(Vector2 target)
        {
            if ((int)wallLayer == 0) return false;
            Vector2 origin = transform.position;
            Vector2 dir    = target - origin;
            return Physics2D.Raycast(origin, dir.normalized, dir.magnitude, wallLayer).collider != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
