using UnityEngine;
using UnityEngine.UI;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 그림자/경보 상태에 따라 화면 가장자리를 어둡게 하는 UI 비네팅.
    // URP Volume과 무관하게 작동. 아무 빈 오브젝트에 붙여도 됨.
    public class VignetteController : MonoBehaviour
    {
        [Header("전환 속도")]
        [SerializeField] private float lerpSpeed = 3f;

        [Header("그림자 비네팅")]
        [SerializeField] private float shadowIntensity = 0.55f;
        [SerializeField] private Color shadowColor     = new Color(0.05f, 0.10f, 0.25f);

        [Header("경계 레벨 비네팅 (추가량)")]
        [SerializeField] private float alertLevel3Extra = 0.20f;
        [SerializeField] private float alertLevel4Extra = 0.40f;
        [SerializeField] private Color alertColor       = new Color(0.18f, 0.04f, 0.04f);

        private Image                 _vigImage;
        private PlayerExposureTracker _tracker;
        private float                 _currentAlpha;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            _tracker = Object.FindAnyObjectByType<PlayerExposureTracker>();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("_VignetteCanvas");
            canvasGO.transform.SetParent(transform);

            var canvas          = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9990;

            canvasGO.AddComponent<CanvasScaler>();

            var imgGO = new GameObject("_VignetteImage");
            imgGO.transform.SetParent(canvasGO.transform, false);

            var rt        = imgGO.AddComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            _vigImage               = imgGO.AddComponent<Image>();
            _vigImage.sprite        = CreateVignetteSprite();
            _vigImage.color         = new Color(0f, 0f, 0f, 0f);
            _vigImage.raycastTarget = false;
        }

        private void Update()
        {
            if (_vigImage == null) return;

            bool inShadow   = _tracker != null && _tracker.IsInShadow;
            int  alertLevel = AlertManager.Instance?.Level ?? 1;

            float baseIntensity  = inShadow ? shadowIntensity : 0f;
            float extraIntensity = alertLevel switch
            {
                3 => alertLevel3Extra,
                4 => alertLevel4Extra,
                _ => 0f,
            };
            float targetAlpha = Mathf.Min(0.90f, baseIntensity + extraIntensity);

            Color baseColor   = inShadow ? shadowColor : Color.black;
            float alertT      = alertLevel4Extra > 0f ? extraIntensity / alertLevel4Extra : 0f;
            Color targetColor = Color.Lerp(baseColor, alertColor, alertT);

            float dt    = Time.deltaTime * lerpSpeed;
            _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, dt);
            targetColor.a = _currentAlpha;

            _vigImage.color = Color.Lerp(_vigImage.color, targetColor, dt);
        }

        private static Sprite CreateVignetteSprite()
        {
            const int size = 256;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx   = (x - half) / half;
                    float dy   = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // 중심부는 투명, 가장자리는 불투명
                    float alpha = Mathf.Clamp01((dist - 0.45f) / 0.55f);
                    alpha = alpha * alpha; // 부드러운 감쇠
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f));
        }
    }
}
