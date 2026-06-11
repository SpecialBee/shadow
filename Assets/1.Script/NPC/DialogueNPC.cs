using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShadowSeller.Core
{
    // NPC 대화 트리거 — E키로 대화 시작.
    // IUsable 없이 독립 동작.
    // CircleCollider2D(isTrigger=true) 필요.
    public class DialogueNPC : MonoBehaviour
    {
        [SerializeField] private DialogueData       dialogueData;
        [SerializeField] private float               detectionRadius = 2f;
        [SerializeField] private TMPro.TMP_FontAsset font;

        private bool              _playerNearby;
        private PlayerController  _player;
        private GameObject        _hintGo;
        private TMPro.TextMeshPro _hintTmp;

        private void Start()
        {
            _player = Object.FindAnyObjectByType<PlayerController>();
            BuildHint();
        }

        private void BuildHint()
        {
            _hintGo = new GameObject("_DialogueHint");
            _hintGo.transform.SetParent(transform);
            _hintGo.transform.localPosition = new Vector3(0f, 1.4f, -0.1f);

            _hintTmp = _hintGo.AddComponent<TMPro.TextMeshPro>();
            _hintTmp.text               = "F 대화";
            _hintTmp.fontSize           = 3f;
            _hintTmp.color              = Color.white;
            _hintTmp.alignment          = TMPro.TextAlignmentOptions.Center;
            _hintTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            _hintTmp.sortingOrder       = 10;
            if (font != null) _hintTmp.font = font;

            _hintGo.SetActive(false);
        }

        private void Update()
        {
            if (_player == null) return;

            float sqrDist = ((Vector2)transform.position - (Vector2)_player.transform.position).sqrMagnitude;
            bool  nearby  = sqrDist <= detectionRadius * detectionRadius;

            if (nearby != _playerNearby)
            {
                _playerNearby = nearby;
                _hintGo?.SetActive(nearby);
            }

            if (!_playerNearby) return;
            if (DialogueSystem.Instance == null) return;
            if (DialogueSystem.Instance.IsPlaying) return;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame)
                DialogueSystem.Instance.StartDialogue(dialogueData);
#else
            if (Input.GetKeyDown(KeyCode.F))
                DialogueSystem.Instance.StartDialogue(dialogueData);
#endif
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
