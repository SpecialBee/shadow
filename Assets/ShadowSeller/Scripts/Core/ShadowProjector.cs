using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트 뒤에 그림자 스프라이트를 생성하고, 같은 위치·크기로 ShadowZone(판정 콜라이더)을 동기화.
    // LateUpdate마다 가장 가까운 PointLightSource 방향의 반대에 배치.
    public class ShadowProjector : MonoBehaviour
    {
        [SerializeField] private float shadowDistance = 0.8f;
        [SerializeField] private float shadowAlpha    = 0.45f;

        private Transform          _shadowTransform;
        private SpriteRenderer     _shadowSR;
        private Rigidbody2D        _shadowRb;
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

            _shadowTransform = go.transform;

            // 시각
            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = sr.sprite;
            _shadowSR.color          = new Color(0f, 0f, 0f, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;

            // 판정 — Kinematic RB + Collider + ShadowZone
            int shadowLayer = LayerMask.NameToLayer("Shadow");
            if (shadowLayer >= 0)
            {
                go.layer = shadowLayer;

                _shadowRb               = go.AddComponent<Rigidbody2D>();
                _shadowRb.bodyType      = RigidbodyType2D.Kinematic;
                _shadowRb.gravityScale  = 0f;
                _shadowRb.sleepMode     = RigidbodySleepMode2D.NeverSleep;

                var col = BuildCollider(go, sr);
                col.isTrigger = true;

                go.AddComponent<ShadowZone>();
            }
        }

        private Collider2D BuildCollider(GameObject go, SpriteRenderer sr)
        {
            var origCol = GetComponent<Collider2D>();
            if (origCol is BoxCollider2D origBox)
            {
                var box    = go.AddComponent<BoxCollider2D>();
                box.size   = origBox.size;
                box.offset = origBox.offset;
                return box;
            }

            // 원본 콜라이더 없으면 스프라이트 바운드 기준 박스
            var fallback    = go.AddComponent<BoxCollider2D>();
            fallback.size   = sr.sprite != null ? sr.sprite.bounds.size : Vector2.one;
            return fallback;
        }

        private void Start()
        {
            _lights = Object.FindObjectsByType<PointLightSource>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

            PointLightSource nearest  = null;
            float            minDist  = float.MaxValue;

            foreach (var l in _lights)
            {
                if (l == null) continue;
                float d = Vector2.Distance(transform.position, l.transform.position);
                if (d < minDist) { minDist = d; nearest = l; }
            }

            if (nearest == null)
            {
                _shadowSR.enabled = false;
                return;
            }

            _shadowSR.enabled = true;

            Vector2 dir  = ((Vector2)transform.position - (Vector2)nearest.transform.position).normalized;
            float   dist = shadowDistance * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(minDist / 6f));
            Vector2 pos  = (Vector2)transform.position + dir * dist;

            // 시각
            _shadowTransform.position   = pos;
            _shadowTransform.localScale = transform.localScale;
            _shadowTransform.rotation   = transform.rotation;

            // 판정 콜라이더 동기화
            if (_shadowRb != null)
                _shadowRb.position = pos;
        }

        private void OnDestroy()
        {
            if (_shadowTransform != null)
                Destroy(_shadowTransform.gameObject);
        }
    }
}
