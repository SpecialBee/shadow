using UnityEngine;

namespace ShadowSeller.Core
{
    // 카메라 추적 — SmoothDamp로 target(플레이어)을 부드럽게 따라감.
    // offset 기본값 Z=-10은 2D 카메라가 씬을 올바르게 렌더링하기 위한 거리.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset   = new Vector3(0f, 0f, -10f);

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position, goal, ref _velocity, smoothTime);
        }
    }
}
