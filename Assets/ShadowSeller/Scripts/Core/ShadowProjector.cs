using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트 뒤에 반투명 검정 그림자 스프라이트를 생성.
    // LateUpdate마다 가장 가까운 PointLightSource 방향의 반대에 배치.
    public class ShadowProjector : MonoBehaviour
    {
        [SerializeField] private float shadowDistance = 0.8f;
        [SerializeField] private float shadowAlpha    = 0.45f;

        private SpriteRenderer    _shadowSR;
        private PointLightSource[] _lights;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var go = new GameObject("_Shadow");
            go.transform.SetParent(transform.parent);
            go.transform.position   = transform.position;
            go.transform.localScale = transform.localScale;
            go.transform.rotation   = transform.rotation;

            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = sr.sprite;
            _shadowSR.color          = new Color(0f, 0f, 0f, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;
        }

        private void Start()
        {
            _lights = Object.FindObjectsByType<PointLightSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

            PointLightSource nearest = null;
            float minDist = float.MaxValue;

            foreach (var l in _lights)
            {
                if (l == null) continue;
                float d = Vector2.Distance(transform.position, l.transform.position);
                if (d < minDist) { minDist = d; nearest = l; }
            }

            if (nearest == null) { _shadowSR.enabled = false; return; }

            _shadowSR.enabled = true;

            Vector2 dir  = ((Vector2)transform.position - (Vector2)nearest.transform.position).normalized;
            float   dist = shadowDistance * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(minDist / 6f));

            _shadowSR.transform.position   = (Vector2)transform.position + dir * dist;
            _shadowSR.transform.localScale  = transform.localScale;
            _shadowSR.transform.rotation    = transform.rotation;
        }

        private void OnDestroy()
        {
            if (_shadowSR != null) Destroy(_shadowSR.gameObject);
        }
    }
}
