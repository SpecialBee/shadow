using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 제한 시간 UI.
    //   시계 아이템 획득 전: 완전히 숨겨짐.
    //   획득 후: 페이드인 + MM:SS 카운트다운.
    //   잔여 시간 50% 이하: 노랑 / 25% 이하: 빨강 + 깜박임.
    //   Canvas 자식에 자동으로 UI를 생성하므로 씬에 빈 GameObject로 붙이면 됩니다.
    public class TimerUI : MonoBehaviour
    {
        [Header("위치 (0=화면 상단 중앙)")]
        [SerializeField] private Vector2 anchoredPos = new Vector2(0f, -60f);
        [SerializeField] private Vector2 panelSize   = new Vector2(160f, 48f);

        [Header("페이드")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("색상")]
        [SerializeField] private Color colorNormal   = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color colorWarning  = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color colorCritical = new Color(1f, 0.2f, 0.2f);

        private CanvasGroup      _group;
        private TextMeshProUGUI  _text;
        private Image            _bg;
        private Coroutine        _fadeRoutine;
        private Coroutine        _blinkRoutine;

        private void Start()
        {
            BuildUI();
            if (_group != null) _group.alpha = 0f;

            TimerManager.OnTimerRevealed += Reveal;
            TimerManager.OnTimerExpired  += OnExpired;
        }

        private void OnDestroy()
        {
            TimerManager.OnTimerRevealed -= Reveal;
            TimerManager.OnTimerExpired  -= OnExpired;
        }

        // ── UI 자동 생성 ─────────────────────────────────────────────────────

        private Canvas FindTargetCanvas()
        {
            // UICanvas 우선
            var go = GameObject.Find("UICanvas");
            if (go != null) { var c = go.GetComponent<Canvas>(); if (c != null && c.isActiveAndEnabled) return c; }

            // 폴백: 활성화된 Canvas 중 가장 먼저 발견된 것
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if (c.renderMode != RenderMode.WorldSpace) return c;

            return FindAnyObjectByType<Canvas>();
        }

        private void BuildUI()
        {
            var canvas = FindTargetCanvas();
            if (canvas == null) { Debug.LogError("[TimerUI] Canvas를 찾을 수 없습니다."); return; }
            Debug.Log($"[TimerUI] 사용 Canvas: {canvas.name}");

            // 패널 루트
            var panelGO       = new GameObject("_TimerPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            panelGO.transform.SetAsLastSibling();

            var panelRT       = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 1f);
            panelRT.anchorMax = new Vector2(0.5f, 1f);
            panelRT.pivot     = new Vector2(0.5f, 1f);
            panelRT.sizeDelta = panelSize;
            panelRT.anchoredPosition = anchoredPos;

            _group        = panelGO.AddComponent<CanvasGroup>();
            _group.interactable   = false;
            _group.blocksRaycasts = false;

            // 배경
            _bg           = panelGO.AddComponent<Image>();
            _bg.color     = new Color(0f, 0f, 0f, 0.55f);

            // 텍스트
            var textGO    = new GameObject("_TimerText", typeof(RectTransform));
            textGO.transform.SetParent(panelGO.transform, false);

            var textRT    = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8f, 4f);
            textRT.offsetMax = new Vector2(-8f, -4f);

            _text              = textGO.AddComponent<TextMeshProUGUI>();
            _text.text         = "15:00";
            _text.fontSize     = 22;
            _text.fontStyle    = FontStyles.Bold;
            _text.alignment    = TextAlignmentOptions.Center;
            _text.color        = colorNormal;
        }

        // ── 매 프레임 갱신 ───────────────────────────────────────────────────

        private void Update()
        {
            if (TimerManager.Instance == null || !TimerManager.Instance.IsRevealed) return;

            float remaining = TimerManager.Instance.Remaining;
            float norm      = TimerManager.Instance.NormalizedLeft;

            int   minutes   = Mathf.FloorToInt(remaining / 60f);
            int   seconds   = Mathf.FloorToInt(remaining % 60f);
            if (_text != null)
                _text.text = $"{minutes:D2}:{seconds:D2}";

            // 색상 전환
            Color target = norm > 0.5f ? colorNormal
                         : norm > 0.25f ? colorWarning
                         : colorCritical;

            if (_text != null && _blinkRoutine == null)
                _text.color = target;

            // 25% 이하 → 깜박임 시작
            if (norm <= 0.25f && _blinkRoutine == null && !TimerManager.Instance.IsExpired)
                _blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        // ── 공개 (시계 아이템 획득) ──────────────────────────────────────────

        private void Reveal()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(1f));
        }

        private void OnExpired()
        {
            if (_blinkRoutine != null) { StopCoroutine(_blinkRoutine); _blinkRoutine = null; }
            if (_text != null) { _text.text = "00:00"; _text.color = colorCritical; }
        }

        // ── 코루틴 ───────────────────────────────────────────────────────────

        private IEnumerator FadeTo(float target)
        {
            float start   = _group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed     += Time.deltaTime;
                _group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }
            _group.alpha  = target;
            _fadeRoutine  = null;
        }

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                if (_text != null) _text.color = colorCritical;
                yield return new WaitForSeconds(0.4f);
                if (_text != null) _text.color = new Color(colorCritical.r, colorCritical.g, colorCritical.b, 0.2f);
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
