using UnityEngine;

namespace ShadowSeller.Core
{
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
