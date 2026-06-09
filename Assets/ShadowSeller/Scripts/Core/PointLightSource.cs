using UnityEngine;

namespace ShadowSeller.Core
{
    // 광원 위치 마커. ShadowProjector가 이 컴포넌트를 기준으로 그림자 방향을 계산.
    // Point Light 2D GameObject에 함께 부착.
    public class PointLightSource : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
