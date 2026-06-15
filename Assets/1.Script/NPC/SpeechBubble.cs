using System.Collections;
using UnityEngine;
using TMPro;

namespace ShadowSeller.Core
{
    // NPC 말풍선 — 배경(BG) + 텍스트 구조.
    // Show(text) 호출 시 텍스트 크기에 맞게 배경 자동 리사이즈 후 duration초 표시, 페이드아웃.
    // bubbleSprite 슬롯에 에셋을 연결하면 교체 가능. 없으면 흰 사각형 플레이스홀더.
    public class SpeechBubble : MonoBehaviour
    {
        [Header("스프라이트 — 없으면 흰 사각형 플레이스홀더")]
        [SerializeField] private Sprite bubbleSprite;

        [Header("레이아웃")]
        [SerializeField] private float xOffset  = 0f;
        [SerializeField] private float yOffset  = 1.4f;
        [SerializeField] private float paddingX = 0.35f;
        [SerializeField] private float paddingY = 0.25f;
        [SerializeField] private float maxWidth = 3.5f;

        [Header("타이밍")]
        [SerializeField] private float duration = 2.2f;
        [SerializeField] private float fadeTime = 0.4f;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset font;

        private GameObject     _bubbleRoot;
        private SpriteRenderer _bgSr;
        private TextMeshPro    _tmp;
        private Coroutine      _routine;

        private void Awake()
        {
            _bubbleRoot = new GameObject("_SpeechBubble");
            _bubbleRoot.transform.SetParent(transform);
            _bubbleRoot.transform.localPosition = new Vector3(xOffset, yOffset, -0.1f);

            var bgGo = new GameObject("_BG");
            bgGo.transform.SetParent(_bubbleRoot.transform);
            bgGo.transform.localPosition = Vector3.zero;
            _bgSr              = bgGo.AddComponent<SpriteRenderer>();
            _bgSr.sprite       = bubbleSprite != null ? bubbleSprite : MakePlaceholder();
            _bgSr.color        = new Color(1f, 1f, 0.92f, 0.95f);
            _bgSr.sortingOrder = 11;

            var textGo = new GameObject("_Text");
            textGo.transform.SetParent(_bubbleRoot.transform);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            _tmp                         = textGo.AddComponent<TextMeshPro>();
            _tmp.fontSize                = 2.6f;
            _tmp.color                   = new Color(0.1f, 0.1f, 0.1f, 1f);
            _tmp.alignment               = TextAlignmentOptions.Center;
            _tmp.textWrappingMode        = TMPro.TextWrappingModes.Normal;
            _tmp.overflowMode            = TextOverflowModes.Overflow;
            _tmp.sortingOrder            = 12;
            _tmp.rectTransform.sizeDelta = new Vector2(maxWidth, 2f);
            if (font != null) _tmp.font  = font;

            _bubbleRoot.SetActive(false);
        }

        private void Start()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                float colTopLocal = col.bounds.max.y - transform.position.y;
                _bubbleRoot.transform.localPosition = new Vector3(xOffset, colTopLocal + yOffset, -0.1f);
            }
        }

        public void Show(string text)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(text));
        }

        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bubbleRoot?.SetActive(false);
        }

        private IEnumerator ShowRoutine(string text)
        {
            _tmp.text = text;
            _tmp.ForceMeshUpdate();

            Vector2 preferred = _tmp.GetPreferredValues(text, maxWidth, Mathf.Infinity);
            float w = preferred.x + paddingX * 2f;
            float h = preferred.y + paddingY * 2f;

            Vector2 bgNatural = _bgSr.sprite != null ? _bgSr.sprite.bounds.size : Vector2.one;
            _bgSr.transform.localScale = new Vector3(w / bgNatural.x, h / bgNatural.y, 1f);

            ApplyAlpha(1f);
            _bubbleRoot.SetActive(true);

            yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeTime));

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                ApplyAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeTime));
                yield return null;
            }

            _bubbleRoot.SetActive(false);
        }

        private void ApplyAlpha(float a)
        {
            Color c;
            c = _bgSr.color; c.a = a * 0.95f; _bgSr.color = c;
            c = _tmp.color;  c.a = a;          _tmp.color  = c;
        }

        private static Sprite MakePlaceholder()
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Bilinear };
            var px  = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        }
    }
}
