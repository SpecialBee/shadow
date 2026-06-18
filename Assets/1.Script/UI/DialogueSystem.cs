using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowSeller.Core
{
    // 하단 대화창 시스템 — 싱글톤
    // DialogueData를 받아 순차적으로 대화 출력.
    // E키 또는 스페이스로 다음 줄 넘기기.
    // 대화 중 PlayerController.IsLocked = true 로 이동 잠금.
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("UI 슬롯")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject      nextIndicator;

        [Header("패널 페이드 — 대화창 루트에 CanvasGroup 추가 후 연결")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private float       panelFadeDuration = 0.2f;

        [Header("타이핑 설정")]
        [SerializeField] private float typeSpeed = 0.04f;

        public bool IsPlaying { get; private set; }

        private DialogueLine[]   _lines;
        private int              _index;
        private Coroutine        _typeRoutine;
        private Coroutine        _panelFadeRoutine;
        private bool             _lineComplete;
        private PlayerController _player;
        private System.Action    _onComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (panelGroup != null) { panelGroup.alpha = 0f; panelGroup.interactable = false; panelGroup.blocksRaycasts = false; }
            HideTexts();
        }

        private void Start()
        {
            _player = Object.FindAnyObjectByType<PlayerController>();
        }

        // ── 공개 API ─────────────────────────────────────────────────────────

        public void StartDialogue(DialogueData data, System.Action onComplete = null)
        {
            if (data == null || data.lines.Length == 0) { onComplete?.Invoke(); return; }
            if (IsPlaying) return;

            _lines      = data.lines;
            _index      = 0;
            IsPlaying   = true;
            _onComplete = onComplete;

            if (_player != null) _player.IsLocked = true;

            ShowTexts();
            FadePanelTo(1f);
            ShowLine(_lines[_index]);
        }

        public void ForceEnd()
        {
            if (!IsPlaying) return;
            if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
            EndDialogue();
        }

        public void Next()
        {
            if (!IsPlaying) return;

            AudioManager.Instance?.PlaySFX(SFXClip.DialogueNext);

            if (!_lineComplete)
            {
                if (_typeRoutine != null) StopCoroutine(_typeRoutine);
                if (dialogueText != null) dialogueText.text = _lines[_index].text;
                _lineComplete = true;
                nextIndicator?.SetActive(true);
                return;
            }

            _index++;
            if (_index >= _lines.Length) { EndDialogue(); return; }
            ShowLine(_lines[_index]);
        }

        // ── 내부 처리 ─────────────────────────────────────────────────────────

        private void ShowTexts()
        {
            nameText?.gameObject.SetActive(true);
            dialogueText?.gameObject.SetActive(true);
            nextIndicator?.SetActive(false);
        }

        private void HideTexts()
        {
            nameText?.gameObject.SetActive(false);
            dialogueText?.gameObject.SetActive(false);
            nextIndicator?.SetActive(false);
        }

        private void FadePanelTo(float target)
        {
            if (panelGroup == null) return;
            if (_panelFadeRoutine != null) StopCoroutine(_panelFadeRoutine);
            _panelFadeRoutine = StartCoroutine(FadePanelRoutine(panelGroup.alpha, target));
        }

        private IEnumerator FadePanelRoutine(float from, float to)
        {
            panelGroup.interactable   = to > 0f;
            panelGroup.blocksRaycasts = to > 0f;
            float elapsed = 0f;
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                panelGroup.alpha = Mathf.Lerp(from, to, elapsed / panelFadeDuration);
                yield return null;
            }
            panelGroup.alpha = to;
            _panelFadeRoutine = null;
        }

        private void ShowLine(DialogueLine line)
        {
            _lineComplete = false;
            nextIndicator?.SetActive(false);

            if (nameText != null) nameText.text = line.speakerName;

            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypeRoutine(line.text));
        }

        private IEnumerator TypeRoutine(string fullText)
        {
            if (dialogueText != null) dialogueText.text = "";
            foreach (char c in fullText)
            {
                if (dialogueText != null) dialogueText.text += c;
                yield return new WaitForSeconds(typeSpeed);
            }
            _lineComplete = true;
            nextIndicator?.SetActive(true);
        }

        private void EndDialogue()
        {
            IsPlaying = false;
            _lines    = null;

            if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
            nextIndicator?.SetActive(false);

            StartCoroutine(EndFadeRoutine());
        }

        private IEnumerator EndFadeRoutine()
        {
            if (panelGroup != null)
            {
                FadePanelTo(0f);
                yield return new WaitUntil(() => _panelFadeRoutine == null);
            }
            HideTexts();
            if (_player != null) _player.IsLocked = false;
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        // ── E키 / 스페이스 / 마우스 좌클릭 입력 ─────────────────────────────────

        private void Update()
        {
            if (!IsPlaying) return;
#if ENABLE_INPUT_SYSTEM
            var kb    = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool pressed = (kb    != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                        || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (pressed) Next();
#else
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                Next();
#endif
        }
    }
}
