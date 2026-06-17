using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    public class QuestUI : MonoBehaviour
    {
        [Header("UI 연결 (비워두면 자동 감지)")]
        [SerializeField] private RectTransform entriesContainer;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup   panelGroup;

        [Header("슬라이드 설정")]
        [SerializeField] private float panelWidth       = 220f;
        [SerializeField] private float slideInDuration  = 0.28f;
        [SerializeField] private float slideOutDuration = 0.32f;

        [Header("항목 설정")]
        [SerializeField] private float entryFadeDuration  = 0.18f;
        [SerializeField] private float flashDuration       = 0.5f;
        [SerializeField] private float completeHoldTime    = 1.6f;
        [SerializeField] private float completeFadeDuration= 0.4f;

        // ── 색상 팔레트 ──────────────────────────────────────────────────
        private static readonly Color ColName      = new Color(1.00f, 0.94f, 0.70f, 1f);
        private static readonly Color ColCount     = new Color(0.65f, 0.65f, 0.65f, 1f);
        private static readonly Color ColCheck     = new Color(0.45f, 0.90f, 0.50f, 1f);
        private static readonly Color ColEmpty     = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color ColFlash     = new Color(1.00f, 0.88f, 0.25f, 1f);
        private static readonly Color ColComplete  = new Color(0.35f, 1.00f, 0.50f, 1f);

        // ── 내부 상태 ────────────────────────────────────────────────────
        private class QuestEntry
        {
            public GameObject      root;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI countText;
            public TextMeshProUGUI checkText;
            public CanvasGroup     group;
            public int             current;
            public int             total;
            public Coroutine       flashCo;
        }

        private readonly Dictionary<string, QuestEntry> _entries = new();
        private Coroutine _slideCo;
        private bool      _panelVisible;

        // ── 초기화 ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (panelRect  == null) panelRect  = GetComponent<RectTransform>();
            if (panelGroup == null) panelGroup = GetComponent<CanvasGroup>();

            // 시작 시 화면 오른쪽 밖에 숨김
            if (panelRect != null)
            {
                var pos = panelRect.anchoredPosition;
                panelRect.anchoredPosition = new Vector2(panelWidth + 30f, pos.y);
            }
            if (panelGroup != null) panelGroup.alpha = 0f;

            BuildHeader();
        }

        private void BuildHeader()
        {
            if (entriesContainer == null) return;

            var hdrGo  = new GameObject("_Header", typeof(RectTransform));
            hdrGo.transform.SetParent(entriesContainer, false);
            hdrGo.transform.SetAsFirstSibling();

            var hdrTmp = hdrGo.AddComponent<TextMeshProUGUI>();
            hdrTmp.text      = "◆ 서브퀘스트";
            hdrTmp.fontSize  = 11f;
            hdrTmp.color     = new Color(0.7f, 0.7f, 0.7f, 0.85f);
            hdrTmp.fontStyle = FontStyles.Bold;
            hdrTmp.raycastTarget = false;

            var le = hdrGo.AddComponent<LayoutElement>();
            le.preferredHeight = 16f;

            // 구분선
            var lineGo = new GameObject("_Divider", typeof(RectTransform));
            lineGo.transform.SetParent(entriesContainer, false);
            lineGo.transform.SetSiblingIndex(1);

            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(1f, 1f, 1f, 0.08f);
            lineImg.raycastTarget = false;

            var lineLE = lineGo.AddComponent<LayoutElement>();
            lineLE.preferredHeight = 1f;
            lineLE.flexibleWidth   = 1f;
        }

        // ── 이벤트 ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            QuestManager.OnQuestActivated += HandleActivated;
            QuestManager.OnQuestProgress  += HandleProgress;
            QuestManager.OnQuestCompleted += HandleCompleted;
        }

        private void OnDisable()
        {
            QuestManager.OnQuestActivated -= HandleActivated;
            QuestManager.OnQuestProgress  -= HandleProgress;
            QuestManager.OnQuestCompleted -= HandleCompleted;
        }

        private void HandleActivated(QuestData data)
        {
            if (_entries.ContainsKey(data.questId)) return;
            _entries[data.questId] = CreateEntry(data);
            if (!_panelVisible) SlideIn();
        }

        private void HandleProgress(QuestData data, int current)
        {
            if (!_entries.TryGetValue(data.questId, out var e)) return;
            e.current = current;
            RefreshEntry(e);
            Flash(e);
        }

        private void HandleCompleted(QuestData data)
        {
            if (!_entries.TryGetValue(data.questId, out var e)) return;
            e.current = data.totalCount;
            RefreshEntry(e);
            StartCoroutine(CompleteRoutine(data.questId, e));
        }

        // ── 항목 생성 ────────────────────────────────────────────────────

        private QuestEntry CreateEntry(QuestData data)
        {
            // 루트 — 배경 포함
            var root = new GameObject(data.questId, typeof(RectTransform));
            root.transform.SetParent(entriesContainer, false);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.25f);
            bg.raycastTarget = false;

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var vl = root.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 5, 5);
            vl.spacing = 3f;
            vl.childControlWidth    = true;
            vl.childControlHeight   = true;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── 이름 + 카운트 행 ─────────────────────────────────────────
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth    = true;
            hl.childControlHeight   = true;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 이름
            var nameGo = MakeTMPGo("Name", row.transform, data.questName, 12.5f, ColName, FontStyles.Bold);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // 카운트
            var countGo = MakeTMPGo("Count", row.transform, $"0/{data.totalCount}", 11.5f, ColCount);
            var cntTmp  = countGo.GetComponent<TextMeshProUGUI>();
            cntTmp.alignment = TextAlignmentOptions.Right;
            var cntLE = countGo.AddComponent<LayoutElement>();
            cntLE.preferredWidth  = 32f;
            cntLE.preferredHeight = 16f;

            // ── 체크박스 행 ──────────────────────────────────────────────
            var checkGo = MakeTMPGo("Check", root.transform, BuildBoxes(0, data.totalCount), 13f, ColEmpty);
            var chLE = checkGo.AddComponent<LayoutElement>();
            chLE.preferredHeight = 16f;

            var entry = new QuestEntry
            {
                root      = root,
                nameText  = nameGo.GetComponent<TextMeshProUGUI>(),
                countText = cntTmp,
                checkText = checkGo.GetComponent<TextMeshProUGUI>(),
                group     = cg,
                current   = 0,
                total     = data.totalCount,
            };

            StartCoroutine(FadeGroup(cg, 0f, 1f, entryFadeDuration));
            return entry;
        }

        private void RefreshEntry(QuestEntry e)
        {
            if (e.countText != null)
                e.countText.text = $"{e.current}/{e.total}";
            if (e.checkText != null)
                e.checkText.text = BuildBoxes(e.current, e.total);
        }

        // ── 플래시 ───────────────────────────────────────────────────────

        private void Flash(QuestEntry e)
        {
            if (e.flashCo != null) StopCoroutine(e.flashCo);
            e.flashCo = StartCoroutine(FlashRoutine(e));
        }

        private IEnumerator FlashRoutine(QuestEntry e)
        {
            SetProgressColor(e, ColFlash);
            yield return new WaitForSeconds(flashDuration * 0.4f);

            float t = 0f, half = flashDuration * 0.6f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = t / half;
                if (e.countText != null) e.countText.color = Color.Lerp(ColFlash, ColCount, p);
                if (e.checkText != null) e.checkText.color = Color.Lerp(ColFlash, ColCheck, p);
                yield return null;
            }
            SetProgressColor(e, default); // 정상 색 복원
            e.flashCo = null;
        }

        private void SetProgressColor(QuestEntry e, Color c)
        {
            bool useDefault = c == default;
            if (e.countText != null) e.countText.color = useDefault ? ColCount : c;
            if (e.checkText != null) e.checkText.color = useDefault ? ColCheck : c;
        }

        // ── 완료 ─────────────────────────────────────────────────────────

        private IEnumerator CompleteRoutine(string questId, QuestEntry e)
        {
            if (e.flashCo != null) { StopCoroutine(e.flashCo); e.flashCo = null; }

            // 전체 초록으로
            if (e.nameText  != null) e.nameText.color  = ColComplete;
            if (e.countText != null) e.countText.color = ColComplete;
            if (e.checkText != null) e.checkText.color = ColComplete;

            // "완료!" 텍스트 추가
            var doneGo = MakeTMPGo("Done", e.root.transform, "✓  완료!", 12f, ColComplete, FontStyles.Bold);
            doneGo.AddComponent<LayoutElement>().preferredHeight = 16f;

            yield return new WaitForSeconds(completeHoldTime);

            // 페이드아웃
            yield return StartCoroutine(FadeGroup(e.group, 1f, 0f, completeFadeDuration));

            _entries.Remove(questId);
            if (e.root != null) Destroy(e.root);

            if (_entries.Count == 0) SlideOut();
        }

        // ── 패널 슬라이드 ────────────────────────────────────────────────

        private void SlideIn()
        {
            _panelVisible = true;
            if (panelGroup != null) panelGroup.alpha = 1f;
            if (_slideCo != null) StopCoroutine(_slideCo);
            float currentX = panelRect != null ? panelRect.anchoredPosition.x : panelWidth + 30f;
            _slideCo = StartCoroutine(SlideRoutine(currentX, -16f, slideInDuration));
        }

        private void SlideOut()
        {
            _panelVisible = false;
            if (_slideCo != null) StopCoroutine(_slideCo);
            float currentX = panelRect != null ? panelRect.anchoredPosition.x : -16f;
            _slideCo = StartCoroutine(SlideOutRoutine(currentX));
        }

        private IEnumerator SlideOutRoutine(float fromX)
        {
            yield return StartCoroutine(SlideRoutine(fromX, panelWidth + 30f, slideOutDuration));
            if (panelGroup != null) panelGroup.alpha = 0f;
        }

        private IEnumerator SlideRoutine(float fromX, float toX, float duration)
        {
            if (panelRect == null) yield break;
            float elapsed = 0f;
            float fromY   = panelRect.anchoredPosition.y;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothOut(Mathf.Clamp01(elapsed / duration));
                panelRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), fromY);
                yield return null;
            }
            panelRect.anchoredPosition = new Vector2(toX, fromY);
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        private static GameObject MakeTMPGo(string name, Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal)
        {
            var go  = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text         = text;
            tmp.fontSize     = size;
            tmp.color        = color;
            tmp.fontStyle    = style;
            tmp.raycastTarget = false;
            return go;
        }

        private static string BuildBoxes(int current, int total)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < total; i++)
            {
                sb.Append(i < current ? "■" : "□");
                if (i < total - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

        // ease-out (빠르게 시작 → 천천히 멈춤)
        private static float SmoothOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
