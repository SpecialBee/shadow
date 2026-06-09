using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트의 그림자 스프라이트(_Shadow GO)를 런타임에 생성하고 매 프레임 위치를 동기화.
    //   - LightSource range 안에 있을 때만 _Shadow GO 활성화 (범위 밖이면 SetActive false)
    //   - 그림자 방향 : 가장 가까운 LightSource 위치 기준으로 반대 방향 투영
    //   - 그림자 길이 : 광원 가장자리에 가까울수록 길어짐 (Lerp 0.6~1.5)
    //   - _Shadow GO 안에 ShadowZone 포함 → 그림자 숨기 판정도 함께 이동
    //   - BringObject가 붙어있고 IsCarried=true 이면 그림자 강제 숨김
    public class ShadowProjector : MonoBehaviour
    {
        [SerializeField] private float shadowDistance = 0.8f;
        [SerializeField] private float shadowAlpha    = 0.45f;

        private Transform      _shadowTransform;
        private SpriteRenderer _shadowSR;
        private Rigidbody2D    _shadowRb;
        private LightSource[]  _lights = System.Array.Empty<LightSource>();
        private InteractableObject _bring;   // 들기 상태 확인용

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

            var shader = Shader.Find("Sprites/Default");
            var mat    = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            mat.color  = new Color(0f, 0f, 0f, shadowAlpha);

            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = sr.sprite;
            _shadowSR.color          = new Color(0f, 0f, 0f, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;
            var unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader != null)
                _shadowSR.material = new Material(unlitShader);

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

            go.SetActive(false);
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
            _bring  = GetComponent<InteractableObject>();
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

            // 들고 있는 중이면 그림자 강제 숨김
            if (_bring != null && _bring.IsCarried)
            {
                _shadowTransform.gameObject.SetActive(false);
                return;
            }

            LightSource nearest = null;
            float       minDist = float.MaxValue;

            foreach (var l in _lights)
            {
                if (l == null || !l.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(transform.position, l.transform.position);
                if (d > l.Range) continue;
                if (l.WallBlocks(transform.position)) continue;
                if (d < minDist) { minDist = d; nearest = l; }
            }

            if (nearest == null)
            {
                _shadowTransform.gameObject.SetActive(false);
                return;
            }

            _shadowTransform.gameObject.SetActive(true);

            Vector2 dir  = ((Vector2)transform.position - (Vector2)nearest.transform.position).normalized;
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
