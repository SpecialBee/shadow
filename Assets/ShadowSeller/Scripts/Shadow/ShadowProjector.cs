using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트의 그림자 스프라이트(_Shadow GO)를 런타임에 생성하고 매 프레임 위치를 동기화.
    //   - LightSource range 안에 있을 때만 _Shadow GO 활성화 (범위 밖이면 SetActive false)
    //   - 그림자 방향 : 가장 가까운 LightSource 위치 기준으로 반대 방향 투영
    //   - 그림자 길이 : 광원 가장자리에 가까울수록 길어짐 (Lerp 0.6~1.5)
    //   - _Shadow GO 안에 ShadowZone 포함 → 그림자 숨기 판정도 함께 이동
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
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one;
            go.transform.localRotation = Quaternion.identity;

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
                if (d > l.Range) continue;                      // 범위 밖 → 무시
                if (l.WallBlocks(transform.position)) continue; // 벽으로 막힘 → 무시
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

            _shadowTransform.position = pos;

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
