using UnityEngine;

namespace ShadowSeller.Core
{
    // 스프라이트 하단 두 꼭짓점을 타원 초점으로 삼아 발 아래 그림자를 생성.
    //   c = 스프라이트 가로 반경 (초점 거리)
    //   a = c + shadowExtend  (가로 반경, 사용자 지정)
    //   b = sqrt(a²-c²)       (세로 반경, 자동 계산)
    //   광원 방향으로 그림자 중심을 살짝 이동시켜 방향감 부여.
    public class ShadowProjector : MonoBehaviour
    {
        [Header("그림자 크기")]
        [Tooltip("그림자가 스프라이트 발 밖으로 얼마나 뻗을지 (월드 단위). 클수록 넓고 두껍게.")]
        [SerializeField] private float shadowExtend = 0.25f;

        [Header("광원 반응")]
        [Tooltip("광원 반대 방향으로 그림자 중심이 얼마나 이동할지. 0 이면 항상 발 중앙.")]
        [SerializeField] private float lightOffsetStrength = 0.12f;

        [Header("판정 범위")]
        [Tooltip("1 = 비주얼과 동일, 0.5 = 절반, 1.5 = 더 크게")]
        [Range(0.1f, 2f)]
        [SerializeField] private float detectionScale = 1f;

        [Header("기타")]
        [SerializeField] private float shadowAlpha      = 0.65f;
        [SerializeField] private bool  createHidingZone = true;

        [Header("그림자 색상")]
        [SerializeField] private Color normalShadowColor = new Color(0.02f, 0f, 0.08f);
        [SerializeField] private Color hidingShadowColor  = new Color(0.05f, 0.1f, 0.4f);

        private Transform      _shadowTransform;
        private SpriteRenderer _shadowSR;
        private EllipseShadow  _hidingZone;
        private LightSource[]  _lights = System.Array.Empty<LightSource>();
        private InteractableObject _bring;
        private Transform      _playerTransform;

        // 스프라이트 로컬 기준값 (Awake 시 한 번만 계산)
        private float _localHalfWidth;   // 스프라이트 가로 반경 (로컬)
        private float _localHalfHeight;  // 스프라이트 세로 반경 (로컬)

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            if (sr.sprite != null)
            {
                _localHalfWidth  = sr.sprite.bounds.extents.x;
                _localHalfHeight = sr.sprite.bounds.extents.y;
            }
            else
            {
                _localHalfWidth  = 0.5f;
                _localHalfHeight = 0.5f;
            }

            var go = new GameObject("_Shadow");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(false);

            _shadowTransform = go.transform;

            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = EllipseShadow.BuildGradientSprite(64, 64f);
            _shadowSR.color          = new Color(normalShadowColor.r, normalShadowColor.g, normalShadowColor.b, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) _shadowSR.material = new Material(shader);

            if (createHidingZone)
            {
                _hidingZone = go.AddComponent<EllipseShadow>();
                _hidingZone.createVisual = false;
            }
        }

        private void Start()
        {
            _lights = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);
            _bring  = GetComponent<InteractableObject>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) _playerTransform = player.transform;
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

            if (_bring != null && _bring.IsCarried)
            {
                _shadowTransform.gameObject.SetActive(false);
                return;
            }

            // 광원 탐색
            LightSource nearest = null;
            float minDist = float.MaxValue;
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

            // ── 타원 계산 ─────────────────────────────────────────────
            // 초점 = 스프라이트 하단 두 꼭짓점 → c = 월드 가로 반경
            float c = _localHalfWidth * Mathf.Abs(transform.lossyScale.x);
            float a = c + shadowExtend;                    // 가로 반경
            float b = Mathf.Sqrt(a * a - c * c);          // 세로 반경 (자동)

            // ── 위치 ─────────────────────────────────────────────────
            // 기준: 스프라이트 하단 중앙 (두 초점의 중점)
            Vector2 bottomCenter = (Vector2)transform.position
                - new Vector2(0f, _localHalfHeight * Mathf.Abs(transform.lossyScale.y));

            // 광원 반대 방향으로 살짝 이동 (광원이 가까울수록 더 이동)
            float   lightT  = 1f - Mathf.Clamp01(minDist / nearest.Range);
            Vector2 toLight = ((Vector2)nearest.transform.position - bottomCenter).normalized;
            Vector2 center  = bottomCenter - toLight * (lightOffsetStrength * lightT);

            // ── 로컬 스케일 변환 (월드 a×b → 부모 스케일 보정) ─────────
            Vector3 ps       = transform.lossyScale;
            float lx = (Mathf.Abs(ps.x) > 0.001f) ? (a * 2f) / ps.x : a * 2f;
            float ly = (Mathf.Abs(ps.y) > 0.001f) ? (b * 2f) / ps.y : b * 2f;

            _shadowTransform.position   = (Vector3)center;
            _shadowTransform.rotation   = Quaternion.identity;
            _shadowTransform.localScale = new Vector3(lx, ly, 1f);

            // 판정 반경 = 타원 반경(a, b)과 일치 = lossyScale * 0.5
            if (_hidingZone != null)
                _hidingZone.detectionRadiusMultiplier = 0.5f * detectionScale;

            // 플레이어 은신 시 그림자 색상 변화
            if (_hidingZone != null && _playerTransform != null)
            {
                bool  hiding   = _hidingZone.ContainsPoint(_playerTransform.position);
                Color col      = hiding ? hidingShadowColor : normalShadowColor;
                float colAlpha = hiding ? Mathf.Min(shadowAlpha + 0.18f, 1f) : shadowAlpha;
                _shadowSR.color = new Color(col.r, col.g, col.b, colAlpha);
            }
        }

        public float CurrentAlpha => _shadowSR != null ? _shadowSR.color.a : 0f;

        public void SetAlpha(float alpha)
        {
            if (_shadowSR != null)
                _shadowSR.color = new Color(normalShadowColor.r, normalShadowColor.g, normalShadowColor.b, Mathf.Clamp01(alpha));
        }

        private void OnDestroy()
        {
            if (_shadowTransform != null)
                Destroy(_shadowTransform.gameObject);
        }
    }
}
