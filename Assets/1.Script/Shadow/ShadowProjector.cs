using UnityEngine;

namespace ShadowSeller.Core
{
    // 오브젝트의 타원형 그림자(_Shadow GO)를 런타임에 생성하고 매 프레임 위치를 동기화.
    //   - LightSource range 안에 있을 때만 _Shadow GO 활성화 (범위 밖이면 SetActive false)
    //   - 그림자 방향 : 가장 가까운 LightSource 위치 기준으로 반대 방향 투영
    //   - 그림자 길이 : 광원 가장자리에 가까울수록 길어짐 (Lerp 0.6~1.5)
    //   - createHidingZone=true : EllipseShadow 컴포넌트를 _Shadow GO에 추가 → 은신 판정 포함
    //   - createHidingZone=false : 시각 전용 그림자 (NPC 등)
    //   - BringObject가 붙어있고 IsCarried=true 이면 그림자 강제 숨김
    public class ShadowProjector : MonoBehaviour
    {
        [SerializeField] private float shadowDistance   = 0.8f;
        [SerializeField] private float shadowAlpha      = 0.75f;
        [SerializeField] private bool  createHidingZone = true;

        [Header("발 기준 그림자")]
        [SerializeField] private float footOffset = 0.3f;   // 스프라이트 중심 아래 발 위치
        [SerializeField] private float shadowFlat = 0.25f;  // 타원 가로 폭 (작을수록 납작)

        private Transform      _shadowTransform;
        private SpriteRenderer _shadowSR;
        private LightSource[]  _lights = System.Array.Empty<LightSource>();
        private InteractableObject _bring;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var go = new GameObject("_Shadow");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one;
            go.transform.localRotation = Quaternion.identity;

            // Awake 순서 제어: 먼저 비활성화한 뒤 컴포넌트 추가
            // → EllipseShadow.Awake()가 GO 활성화 시점에 실행되어 createVisual 설정이 반영됨
            go.SetActive(false);

            _shadowTransform = go.transform;

            // 그라데이션 스프라이트를 원본 스프라이트와 동일한 월드 크기로 생성
            // → 원본 PPU 계산: 64px / 원본최대크기(월드단위) = 동일 시각 범위
            float srcMax = sr.sprite != null
                ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y, 0.01f)
                : 1f;
            float gradPPU = 64f / srcMax;

            _shadowSR                = go.AddComponent<SpriteRenderer>();
            _shadowSR.sprite         = EllipseShadow.BuildGradientSprite(64, gradPPU);
            _shadowSR.color          = new Color(0f, 0f, 0f, shadowAlpha);
            _shadowSR.sortingLayerID = sr.sortingLayerID;
            _shadowSR.sortingOrder   = sr.sortingOrder - 1;
            var unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader != null)
                _shadowSR.material = new Material(unlitShader);

            if (createHidingZone)
            {
                var es = go.AddComponent<EllipseShadow>();
                es.createVisual = false;
                // 판정 반경은 LateUpdate에서 설정하는 lossyScale을 자동으로 사용
            }
        }

        private void Start()
        {
            _lights = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);
            _bring  = GetComponent<InteractableObject>();
        }

        private void LateUpdate()
        {
            if (_shadowSR == null) return;

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

            Vector2 footPos = (Vector2)transform.position - new Vector2(0f, footOffset);
            Vector2 dir     = (footPos - (Vector2)nearest.transform.position).normalized;

            float t      = Mathf.Clamp01(minDist / nearest.Range);
            float length = shadowDistance * Mathf.Lerp(0.6f, 1.5f, t);

            Vector2 shadowCenter = footPos + dir * (length * 0.5f);
            float   angle        = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            // localScale.x = 가로 폭(shadowFlat), localScale.y = 세로 길이(length)
            // EllipseShadow(createVisual=false)가 이 lossyScale을 판정 반경으로 자동 사용
            _shadowTransform.position   = shadowCenter;
            _shadowTransform.rotation   = Quaternion.Euler(0f, 0f, angle);
            _shadowTransform.localScale = new Vector3(shadowFlat, length, 1f);
        }

        private void OnDestroy()
        {
            if (_shadowTransform != null)
                Destroy(_shadowTransform.gameObject);
        }
    }
}
