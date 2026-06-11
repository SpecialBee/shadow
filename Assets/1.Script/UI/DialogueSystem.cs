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
        [SerializeField] private GameObject      dialoguePanel;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject      nextIndicator;

        [Header("타이핑 설정")]
        [SerializeField] private float typeSpeed = 0.04f;

        public bool IsPlaying { get; private set; }

        private DialogueLine[]   _lines;
        private int              _index;
        private Coroutine        _typeRoutine;
        private bool             _lineComplete;
        private PlayerController _player;
        private System.Action    _onComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            dialoguePanel?.SetActive(false);
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

            dialoguePanel?.SetActive(true);
            ShowLine(_lines[_index]);
        }

        public void Next()
        {
            if (!IsPlaying) return;

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

            dialoguePanel?.SetActive(false);
            if (_player != null) _player.IsLocked = false;

            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        // ── E키 / 스페이스 입력 ───────────────────────────────────────────────

        private void Update()
        {
            if (!IsPlaying) return;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                Next();
#else
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                Next();
#endif
        }
    }
}
