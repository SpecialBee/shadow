using System.Collections;
using UnityEngine;

namespace ShadowSeller.Core
{
    // 밀기 오브젝트: 플레이어가 4방향 중 한 면에 접근하면 반대 방향으로 밀기.
    //   - 플레이어가 왼쪽에 있으면 "F 오른쪽으로 밀기" 힌트 표시
    //   - F키로 밀기 실행 → 슬라이딩 이동, 이동 중 플레이어 이동 잠금
    //   - 이동 경로에 Wall/Door 태그 오브젝트가 있으면 이동 불가
    //   - 이동 완료 후 플레이어 이동 잠금 해제
    //   - weight: 추후 스태미나 소모량 계산에 사용 예정 (현재 미사용)
    public class PushObject : MonoBehaviour, IUsable
    {
        [SerializeField] private float pushDistance  = 2f;    // 한 번에 밀리는 거리
        [SerializeField] private float slideSpeed    = 4f;    // 슬라이딩 속도
        [SerializeField] private float detectionRange = 1.2f; // 방향 판정 범위
        [SerializeField] public  float weight        = 2f;    // 추후 스태미나용 (현재 미사용)

        private bool             _isSliding = false;
        private PlayerController _nearbyPlayer;
        private Vector2          _pushDir;   // 현재 감지된 밀기 방향

        // ── IUsable ───────────────────────────────────────────────────────────

        public string UseHint => GetHintText();

        public void OnUse(PlayerController user)
        {
            if (_isSliding) return;

            _nearbyPlayer = user;
            _pushDir      = GetPushDir(user);

            // 이동 경로에 Wall/Door 있으면 막기
            if (IsBlocked(_pushDir))
            {
                Debug.Log("[PushObject] 경로 막힘 — 이동 불가");
                return;
            }

            StartCoroutine(SlideRoutine(user, _pushDir));
        }

        // ── 슬라이딩 코루틴 ──────────────────────────────────────────────────

        private IEnumerator SlideRoutine(PlayerController player, Vector2 dir)
        {
            _isSliding       = true;
            player.IsLocked  = true;   // 플레이어 이동 잠금

            Vector2 startPos = transform.position;
            Vector2 endPos   = startPos + dir * pushDistance;
            float   elapsed  = 0f;
            float   duration = pushDistance / slideSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                // EaseOut — 끝에 살짝 감속
                float smoothT = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = Vector2.Lerp(startPos, endPos, smoothT);
                yield return null;
            }

            transform.position = endPos;   // 최종 위치 보정

            player.IsLocked = false;       // 플레이어 이동 잠금 해제
            _isSliding       = false;
        }

        // ── 방향 판정 ─────────────────────────────────────────────────────────

        // 플레이어가 어느 쪽에 있는지 → 4방향 중 하나로 스냅
        private Vector2 GetPushDir(PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float   ax       = Mathf.Abs(toPlayer.x);
            float   ay       = Mathf.Abs(toPlayer.y);

            if (ax >= ay)
                return toPlayer.x > 0 ? Vector2.right : Vector2.left;
            else
                return toPlayer.y > 0 ? Vector2.up : Vector2.down;
        }

        // 힌트 텍스트: 플레이어 위치 기준으로 밀기 방향 표시
        private string GetHintText()
        {
            if (_isSliding) return "";
            if (_nearbyPlayer == null) return "F 밀기";

            var dir = GetPushDir(_nearbyPlayer);
            if (dir == Vector2.right) return "F 오른쪽으로 밀기";
            if (dir == Vector2.left)  return "F 왼쪽으로 밀기";
            if (dir == Vector2.up)    return "F 위쪽으로 밀기";
            if (dir == Vector2.down)  return "F 아래쪽으로 밀기";
            return "F 밀기";
        }

        // 이동 경로에 Wall/Door 태그 오브젝트가 있는지 Raycast로 확인
        private bool IsBlocked(Vector2 dir)
        {
            var hits = Physics2D.RaycastAll(transform.position, dir, pushDistance);
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;
                var tag = hit.collider.tag;
                if (tag == "Wall" || tag == "Door") return true;
            }
            return false;
        }

        // ── 범위 감지 ─────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pi = other.GetComponent<PlayerInteraction>();
            if (pi == null) return;

            _nearbyPlayer = other.GetComponent<PlayerController>();
            pi.SetNearbyUsable(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pi = other.GetComponent<PlayerInteraction>();
            if (pi == null) return;

            _nearbyPlayer = null;
            pi.ClearNearbyUsable(this);
        }

        // ── 에디터 Gizmo ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = _isSliding
                ? new Color(0.9f, 0.3f, 0.1f, 0.35f)
                : new Color(0.7f, 0.7f, 0.9f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
