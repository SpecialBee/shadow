using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowSeller.UI
{
    public enum InteractionType { Carry, Push, Pull, Door, Light, Pickup, Talk, Examine }

    public class InteractionPanel : MonoBehaviour
    {
        public static InteractionPanel Instance { get; private set; }

        [Header("버튼 슬롯")]
        [SerializeField] private Button carryBtn;
        [SerializeField] private Button pushBtn;
        [SerializeField] private Button pullBtn;
        [SerializeField] private Button doorBtn;
        [SerializeField] private Button lightBtn;
        [SerializeField] private Button pickupBtn;
        [SerializeField] private Button talkBtn;
        [SerializeField] private Button examineBtn;

        [Header("투명도")]
        [SerializeField] [Range(0f, 1f)] private float activeAlpha = 1f;
        [SerializeField] [Range(0f, 1f)] private float inactiveAlpha = 0.25f;

        [Header("강조 색상")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pulseColor  = new Color(1f, 0.92f, 0.3f);

        public bool IsVisible { get; private set; }

        private readonly List<Transform> _activeButtonTransforms = new List<Transform>();
        private readonly List<Image>     _activeButtonImages     = new List<Image>();
        private Coroutine _pulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DimAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(List<(InteractionType type, string label, System.Action callback)> actions)
        {
            gameObject.SetActive(true);
            DimAll();
            if (actions == null || actions.Count == 0) return;

            bool canAnimate = gameObject.activeInHierarchy;

            foreach (var (type, label, cb) in actions)
            {
                var btn = GetBtn(type);
                if (btn == null) continue;

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;

                var captured = cb;
                SetButtonState(btn, true, captured);
                _activeButtonTransforms.Add(btn.transform);
                _activeButtonImages.Add(btn.GetComponent<Image>());

                if (canAnimate)
                    StartCoroutine(PopScale(btn.transform));
            }

            if (canAnimate)
            {
                if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(PulseActiveButtons());
            }

            IsVisible = true;
        }

        private IEnumerator PopScale(Transform t)
        {
            t.localScale = Vector3.zero;
            float elapsed = 0f;

            // 0 → 1.25 (팝업)
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(0f, 1.25f, elapsed / 0.12f);
                t.localScale = Vector3.one * s;
                yield return null;
            }

            // 1.25 → 1.0 (안착)
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(1.25f, 1f, elapsed / 0.08f);
                t.localScale = Vector3.one * s;
                yield return null;
            }

            t.localScale = Vector3.one;
        }

        private IEnumerator PulseActiveButtons()
        {
            // 팝 애니메이션 완료 대기
            yield return new WaitForSeconds(0.22f);

            while (true)
            {
                float elapsed = 0f;
                const float duration = 0.75f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float s = 1f + 0.06f * Mathf.Sin(t * Mathf.PI);
                    Color c = Color.Lerp(normalColor, pulseColor, Mathf.Sin(t * Mathf.PI));

                    for (int i = 0; i < _activeButtonTransforms.Count; i++)
                    {
                        if (_activeButtonTransforms[i] != null)
                            _activeButtonTransforms[i].localScale = Vector3.one * s;
                        if (i < _activeButtonImages.Count && _activeButtonImages[i] != null)
                            _activeButtonImages[i].color = c;
                    }
                    yield return null;
                }

                for (int i = 0; i < _activeButtonTransforms.Count; i++)
                {
                    if (_activeButtonTransforms[i] != null)
                        _activeButtonTransforms[i].localScale = Vector3.one;
                    if (i < _activeButtonImages.Count && _activeButtonImages[i] != null)
                        _activeButtonImages[i].color = normalColor;
                }

                yield return new WaitForSeconds(0.4f);
            }
        }

        public void Hide()
        {
            DimAll();
            IsVisible = false;
        }

        // ── 내부 ────────────────────────────────────────────────────────────────

        private void DimAll()
        {
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
            _activeButtonTransforms.Clear();
            _activeButtonImages.Clear();

            foreach (var btn in AllBtns())
            {
                if (btn == null) continue;
                SetButtonState(btn, false, null);
                btn.transform.localScale = Vector3.one;
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = normalColor;
            }
            IsVisible = false;
        }

        private void SetButtonState(Button btn, bool on, System.Action callback)
        {
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

            cg.alpha          = on ? activeAlpha : inactiveAlpha;
            cg.interactable   = on;
            cg.blocksRaycasts = on;

            btn.onClick.RemoveAllListeners();
            if (on && callback != null)
            {
                var cap = callback;
                btn.onClick.AddListener(() => cap?.Invoke());
            }
        }

        private Button GetBtn(InteractionType t) => t switch
        {
            InteractionType.Carry  => carryBtn,
            InteractionType.Push   => pushBtn,
            InteractionType.Pull   => pullBtn,
            InteractionType.Door   => doorBtn,
            InteractionType.Light  => lightBtn,
            InteractionType.Pickup => pickupBtn,
            InteractionType.Talk    => talkBtn,
            InteractionType.Examine => examineBtn,
            _                       => null,
        };

        private IEnumerable<Button> AllBtns()
        {
            yield return carryBtn; yield return pushBtn;   yield return pullBtn;
            yield return doorBtn;  yield return lightBtn;  yield return pickupBtn;
            yield return talkBtn;  yield return examineBtn;
        }
    }
}
