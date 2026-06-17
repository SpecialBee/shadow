using UnityEngine;

namespace ShadowSeller.Core
{
    // 플레이어가 닿는 순간 CameraFollow를 끄고 카메라를 특정 좌표로 이동.
    // 오브젝트에 Collider2D (isTrigger=true) 필요.
    public class CameraDetachTrigger : MonoBehaviour
    {
        [Header("카메라 이동 목표 위치 (X, Y만 사용)")]
        [SerializeField] private Vector2 targetPosition;

        [Header("이동 속도 (0이면 즉시 이동)")]
        [SerializeField] private float moveSpeed = 5f;

        private Camera      _cam;
        private CameraFollow _follow;
        private bool        _active = false;

        private void Awake()
        {
            _cam    = Camera.main;
            _follow = _cam?.GetComponent<CameraFollow>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_active) return;

            _active = true;

            // CameraFollow 끄기
            if (_follow != null) _follow.enabled = false;
        }

        private void Update()
        {
            if (!_active || _cam == null) return;

            Vector3 goal = new Vector3(targetPosition.x, targetPosition.y, _cam.transform.position.z);

            if (moveSpeed <= 0f)
            {
                _cam.transform.position = goal;
                _active = false;
                return;
            }

            _cam.transform.position = Vector3.MoveTowards(
                _cam.transform.position, goal, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(_cam.transform.position, goal) < 0.01f)
            {
                _cam.transform.position = goal;
                _active = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawSphere(new Vector3(targetPosition.x, targetPosition.y, 0f), 0.3f);
            Gizmos.DrawLine(transform.position, new Vector3(targetPosition.x, targetPosition.y, 0f));
        }
    }
}
