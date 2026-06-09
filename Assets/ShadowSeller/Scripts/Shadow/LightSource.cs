using UnityEngine;

namespace ShadowSeller.Core
{
    // ┌─────────────────────────────────────────────────────────────────────┐
    // │ LightSource                                                         │
    // │                                                                     │
    // │ 역할 1. 플레이어 Lit 판정                                           │
    // │   CircleCollider2D(trigger)로 플레이어가 범위 안에 들어오면         │
    // │   PlayerExposureTracker에 알려 ExposureState를 Lit으로 올림.        │
    // │   → 의심도 상승 속도 증가                                           │
    // │                                                                     │
    // │ 역할 2. 그림자 생성 기준                                            │
    // │   ShadowProjector가 Range 값을 참조해서,                           │
    // │   오브젝트가 이 범위 안에 있을 때만 그림자 스프라이트를 생성함.     │
    // │   방향도 이 LightSource의 위치 기준으로 계산.                       │
    // │                                                                     │
    // │ Light2D는 별도로 수동 조절. 이 스크립트가 건드리지 않음.           │
    // └─────────────────────────────────────────────────────────────────────┘
    [RequireComponent(typeof(CircleCollider2D))]
    public class LightSource : MonoBehaviour
    {
        // ShadowProjector가 그림자 생성 여부·방향 계산에 사용하는 범위값.
        // CircleCollider2D radius는 인스펙터에서 수동으로 맞출 것.
        [SerializeField] private float range = 5f;
        public float Range => range;

        // 플레이어가 범위 진입 → ExposureState: Lit (의심도 상승)
        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerExposureTracker>()?.OnEnterDangerZone();
        }

        // 플레이어가 범위 이탈 → Lit 해제
        private void OnTriggerExit2D(Collider2D other)
        {
            other.GetComponent<PlayerExposureTracker>()?.OnExitDangerZone();
        }

        // 에디터 씬 뷰에서 범위 시각화 (노란 원)
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
