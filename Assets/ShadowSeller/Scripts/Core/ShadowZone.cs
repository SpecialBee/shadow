using UnityEngine;

namespace ShadowSeller.Core
{
    // 판정용 그림자 영역. 렌더 그림자와 완전 분리.
    // PolygonCollider2D 또는 BoxCollider2D를 함께 배치하고 isTrigger=true 로 설정.
    [RequireComponent(typeof(Collider2D))]
    public class ShadowZone : MonoBehaviour
    {
        [SerializeField] private ExposureState grade = ExposureState.ShadowB;
        public ExposureState Grade => grade;
        public void SetGrade(ExposureState g) { grade = g; }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = grade switch
            {
                ExposureState.ShadowA => new Color(0.1f, 0.2f, 0.8f, 0.35f),
                ExposureState.ShadowB => new Color(0.1f, 0.5f, 0.8f, 0.30f),
                ExposureState.ShadowC => new Color(0.4f, 0.4f, 0.5f, 0.28f),
                ExposureState.ShadowD => new Color(0.5f, 0.3f, 0.1f, 0.25f),
                _                     => new Color(0f, 0f, 0f, 0.2f),
            };

            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
                Gizmos.DrawCube(transform.position + (Vector3)(Vector2)box.offset, box.size);
            else if (col is CircleCollider2D circle)
                Gizmos.DrawSphere(transform.position, circle.radius);
        }
    }
}
