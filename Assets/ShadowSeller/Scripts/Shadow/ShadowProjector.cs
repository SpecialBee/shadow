using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트 뒤에 그림자 스프라이트를 생성하고, 같은 위치·크기로 ShadowZone(판정 콜라이더)을 동기화.
    // 오브젝트가 LightSource의 range 안에 있을 때만 그림자 생성.
    public class ShadowProjector : MonoBehaviour
    {
        [SerializeField] private float shadowDistance = 0.8f;
        [SerializeField] private float shadowAlpha    = 0.45f;

        private Transform     _shadowTransform;
        private SpriteRenderer _shadowSR;
        private Rigidbody2D   _shadowRb;
        private LightSource[]  _lights = System.Array.Empty<LightSource>();

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

            // 시각 — Unlit 재질로 Light2D 영향 차단, LightSource range로만 표시 제어
            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = sr.sprite;
            _shadowSR.color          = new Color(0f, 0f, 0f, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;
            var unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader != null)
                _shadowSR.material = new Material(unlitShader);

            // 판정 — Kinematic RB + Collider + ShadowZone
            int shadowLayer = LayerMask.NameToLayer("Shadow");
            if (shadowLayer >= 0)
            {
                go.layer = shadowLayer;

                _shadowRb              = go.AddComponent<Rigidbody2D>();
                _shadowRb.bodyType     = RigidbodyType2D.Kinematic;
                _shadowRb.gravityScale = 0f;
                _shadowRb.sleepMode    = RigidbodySleepMode2D.NeverSleep;

                var col = BuildCollider(go, sr);
                col.isTrigger = true;

                go.AddComponent<ShadowZone>();
            }

            go.SetActive(false);   // LightSource 범위 진입 전까지 숨김
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

            var fallback  = go.AddComponent<BoxCollider2D>();
            fallback.size = sr.sprite != null ? sr.sprite.bounds.size : Vector2.one;
            return fallback;
        }

        private void Start()
        {
            _lights = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

            // range 안에 있는 광원 중 가장 가까운 것 선택
            LightSource nearest = null;
            float       minDist = float.MaxValue;

            foreach (var l in _lights)
            {
                if (l == null || !l.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(transform.position, l.transform.position);
                if (d > l.Range) continue;   // 범위 밖 → 이 광원은 무시
                if (d < minDist) { minDist = d; nearest = l; }
            }

            if (nearest == null)
            {
                // 어느 광원 범위에도 없음 → 그림자 GO 전체 숨김 (SR + ShadowZone 콜라이더 동시)
                _shadowTransform.gameObject.SetActive(false);
                return;
            }

            _shadowTransform.gameObject.SetActive(true);

            // 광원 중심 → 오브젝트 방향의 반대가 그림자 방향
            Vector2 dir  = ((Vector2)transform.position - (Vector2)nearest.transform.position).normalized;
            // range 가장자리일수록 그림자가 길어짐
            float   dist = shadowDistance * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(minDist / nearest.Range));
            Vector2 pos  = (Vector2)transform.position + dir * dist;

            _shadowTransform.position   = pos;
            _shadowTransform.localScale = transform.localScale;
            _shadowTransform.rotation   = transform.rotation;

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
