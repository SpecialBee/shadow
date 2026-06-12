using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace ShadowSeller.Core
{
    public enum PrologueTriggerMode
    {
        Interaction,  // A: 플레이어 근접 + E키 → (선택 대화) → 챕터 진행
        Zone,         // B: 플레이어가 Trigger 콜라이더 진입 → (선택 대화) → 챕터 진행
        Dialogue,     // C: 플레이어 근접 + E키 → 대화 (필수) 완료 → 챕터 진행
    }

    // 프롤로그 챕터 진행 트리거 — 씬에 배치해 사용.
    //   Interaction : 근접 후 E 키 입력 시 발동 (1회)
    //   Zone        : Trigger 콜라이더에 플레이어 진입 시 발동 → Collider2D + Is Trigger 필요
    //   Dialogue    : 근접 후 E 키 → dialogueData 재생 → 완료 시 발동
    public class PrologueTrigger : MonoBehaviour
    {
        [Header("트리거 종류")]
        [SerializeField] private PrologueTriggerMode mode = PrologueTriggerMode.Interaction;

        [Header("Interaction / Dialogue 모드 — 플레이어 감지 반경")]
        [SerializeField] private float approachRadius = 2f;

        [Header("대화 (Dialogue 모드는 필수 / 나머지는 선택)")]
        [SerializeField] private DialogueData dialogueData;

        [Tooltip("말풍선이 표시될 화자 Transform (null이면 플레이어 위)")]
        [SerializeField] private Transform    dialogueSpeaker;

        [Header("근접 힌트 UI (없으면 비활성)")]
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private string          hintMessage = "E — 상호작용";

        // ── 런타임 ────────────────────────────────────────────────────────────
        private bool      _fired      = false;
        private bool      _playerNear = false;
        private Transform _playerTr;

        private void Start()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null) _playerTr = player.transform;
            ShowHint(false);
        }

        private void Update()
        {
            if (_fired || _playerTr == null) return;
            if (mode == PrologueTriggerMode.Zone) return;

            // 근접 여부 갱신
            float sqr  = ((Vector2)transform.position - (Vector2)_playerTr.position).sqrMagnitude;
            bool  near = sqr <= approachRadius * approachRadius;

            if (near != _playerNear)
            {
                _playerNear = near;
                ShowHint(near);
            }

            if (!near) return;

            // Space 키 입력 (New Input System)
            if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame) return;

            Fire();
        }

        // Zone 모드 전용 — Collider2D (Is Trigger = true) 필요
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired) return;
            if (mode != PrologueTriggerMode.Zone) return;
            if (!other.CompareTag("Player")) return;

            Fire();
        }

        // ── 발동 ──────────────────────────────────────────────────────────────

        private void Fire()
        {
            if (_fired) return;
            _fired = true;
            ShowHint(false);

            var ds = DialogueSystem.Instance;

            if (mode == PrologueTriggerMode.Dialogue)
            {
                // 대화 필수 — 패널 재생 후 챕터 진행
                if (dialogueData != null && ds != null)
                {
                    ds.StartDialogue(dialogueData,
                        () => PrologueDirector.Instance?.AdvanceChapter());
                }
                else
                {
                    Debug.LogWarning($"[PrologueTrigger] Dialogue 모드이지만 dialogueData 또는 DialogueSystem이 없습니다 ({gameObject.name})");
                    PrologueDirector.Instance?.AdvanceChapter();
                }
            }
            else
            {
                // Interaction / Zone: 대화가 있으면 먼저 재생 후 진행, 없으면 즉시
                if (dialogueData != null && ds != null)
                {
                    ds.StartDialogue(dialogueData,
                        () => PrologueDirector.Instance?.AdvanceChapter());
                }
                else
                {
                    PrologueDirector.Instance?.AdvanceChapter();
                }
            }
        }

        private void ShowHint(bool visible)
        {
            if (hintText == null) return;
            hintText.gameObject.SetActive(visible);
            if (visible) hintText.text = hintMessage;
        }

        // ── 에디터 기즈모 ─────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            switch (mode)
            {
                case PrologueTriggerMode.Interaction:
                case PrologueTriggerMode.Dialogue:
                    Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
                    Gizmos.DrawWireSphere(transform.position, approachRadius);
                    break;
                case PrologueTriggerMode.Zone:
                    Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
                    Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
                    break;
            }
        }
    }
}
