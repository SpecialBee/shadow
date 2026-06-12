using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShadowSeller.UI;

namespace ShadowSeller.Core
{
    public class InteractableObject : MonoBehaviour
    {
        [Header("상호작용 종류")]
        [SerializeField] private bool canCarry;
        [SerializeField] private bool canPush;
        [SerializeField] private bool canPull;
        [SerializeField] private bool isDoor;
        [SerializeField] private bool canToggleLight;
        [SerializeField] private bool canInventory;
        [SerializeField] private bool isTarget;

        [Header("들기 설정")]
        [SerializeField] private float holdOffset = 0.7f;

        [Header("밀기 설정")]
        [SerializeField] private float pushDistance = 2f;
        [SerializeField] private float pushSpeed    = 4f;

        [Header("당기기 설정")]
        [SerializeField] private float pullDistance = 2f;
        [SerializeField] private float pullSpeed    = 4f;

        [Header("문 설정")]
        [SerializeField] private Collider2D     doorCollider;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite         openSprite;
        [SerializeField] private Sprite         closedSprite;
        [SerializeField] private bool           startOpen;

        [Header("켜기/끄기 설정")]
        [SerializeField] private LightSource[] controlledSources;

        [Header("인벤토리 설정")]
        [SerializeField] private string itemName = "";

        [Header("대화 설정")]
        [SerializeField] private DialogueData dialogue;

        [Header("접근 강조")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField][Range(0f, 1f)] private float highlightAlpha = 1f;
        [SerializeField] private float approachRadius = 2f;

        [Header("방향 지시자 (선택)")]
        [Tooltip("비워두면 Push/Pull 오브젝트에 자동 생성됩니다")]
        [SerializeField] private TMPro.TextMeshPro dirArrow;

        public bool IsCarried { get; private set; }

        private SpriteRenderer   _sr;
        private Color            _originalColor;
        private PlayerController _player;
        private PlayerController _nearbyPlayer;
        private PlayerController _carrier;
        private bool             _playerNearby;
        private bool             _isOpen;
        private bool             _lightsOn = true;
        private bool             _isSliding;

        private static InteractableObject s_carried;
        private static InteractableObject s_panelOwner;

        // ── 초기화 ───────────────────────────────────────────────────────────────

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
            if (isDoor) ApplyDoorState(startOpen);
        }

        private void Start()
        {
            _player = Object.FindAnyObjectByType<PlayerController>();
            if (canToggleLight && controlledSources != null
                && controlledSources.Length > 0 && controlledSources[0] != null)
                _lightsOn = controlledSources[0].gameObject.activeSelf;

            EnsureDirArrow();
        }

        private void OnDestroy()
        {
            if (s_carried    == this) { s_carried = null; IsCarried = false; }
            if (s_panelOwner == this) HidePanel();
        }

        // ── 접근 감지 & 하이라이트 ───────────────────────────────────────────────

        private void UpdateApproach()
        {
            if (_player == null) return;
            float sqr = ((Vector2)transform.position - (Vector2)_player.transform.position).sqrMagnitude;
            bool nearby = sqr <= approachRadius * approachRadius;
            if (nearby == _playerNearby) return;

            _playerNearby = nearby;
            _nearbyPlayer = nearby ? _player : null;
            SetHighlight(nearby);

            if (nearby)
                ShowPanel();
            else
                HidePanel();
        }

        private void SetHighlight(bool on)
        {
            if (_sr == null) return;
            _sr.color = on
                ? new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightAlpha)
                : _originalColor;
        }

        // ── 업데이트 ─────────────────────────────────────────────────────────────

        private void Update()
        {
            UpdateApproach();

            if (_playerNearby && (canPush || canPull))
                UpdateDirArrow();
        }

        private void LateUpdate()
        {
            if (!IsCarried || _carrier == null) return;
            transform.position = (Vector2)_carrier.transform.position + _carrier.LastMoveDir * holdOffset;
        }

        // ── 패널 표시 / 숨김 ─────────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (_isSliding) return;
            s_panelOwner = this;
            InteractionPanel.Instance?.Show(CollectActions());

            if ((canPush || canPull) && dirArrow != null)
            {
                UpdateDirArrow();
                dirArrow.gameObject.SetActive(true);
            }
        }

        private void HidePanel()
        {
            if (s_panelOwner != this) return;
            s_panelOwner = null;
            InteractionPanel.Instance?.Hide();

            if (dirArrow != null) dirArrow.gameObject.SetActive(false);
        }

        // ── 액션 목록 ────────────────────────────────────────────────────────────

        private List<(InteractionType, string, System.Action)> CollectActions()
        {
            var list = new List<(InteractionType, string, System.Action)>();

            if (IsCarried)
            {
                list.Add((InteractionType.Carry, "내려놓기", DoDrop));
                return list;
            }

            if (canCarry)        list.Add((InteractionType.Carry,  "들기",                       DoCarry));
            if (canPush)         list.Add((InteractionType.Push,   "밀기",                       DoPushAction));
            if (canPull)         list.Add((InteractionType.Pull,   "당기기",                     DoPullAction));
            if (isDoor)          list.Add((InteractionType.Door,   _isOpen ? "닫기" : "열기",    DoToggleDoor));
            if (canToggleLight)  list.Add((InteractionType.Light,  _lightsOn ? "끄기" : "켜기",  DoToggleLight));
            if (canInventory)    list.Add((InteractionType.Pickup, "줍기",                       DoAddToInventory));
            if (isTarget)
            {
                bool done = ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsComplete;
                list.Add((InteractionType.Talk, done ? "완료됨" : "대화", DoTarget));
            }

            return list;
        }

        // ── 방향 지시자 ──────────────────────────────────────────────────────────

        private void EnsureDirArrow()
        {
            if (dirArrow != null || (!canPush && !canPull)) return;

            var go = new GameObject("_DirectionArrow");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.up * 0.75f;
            go.transform.localScale    = Vector3.one;

            dirArrow               = go.AddComponent<TMPro.TextMeshPro>();
            dirArrow.fontSize      = 4f;
            dirArrow.alignment     = TMPro.TextAlignmentOptions.Center;
            dirArrow.color         = new Color(1f, 0.95f, 0.2f);
            dirArrow.sortingOrder  = 10;

            go.SetActive(false);
        }

        private void UpdateDirArrow()
        {
            if (dirArrow == null || _nearbyPlayer == null) return;
            bool usePull = !canPush && canPull;
            Vector2 dir  = GetSnapDir(_nearbyPlayer, fromPlayer: usePull);
            dirArrow.text = DirToArrow(dir);
        }

        private static string DirToArrow(Vector2 dir)
        {
            if (dir == Vector2.up)    return "↑";
            if (dir == Vector2.down)  return "↓";
            if (dir == Vector2.left)  return "←";
            if (dir == Vector2.right) return "→";
            return "";
        }

        // ── 들기 / 내려놓기 ──────────────────────────────────────────────────────

        private void DoCarry()
        {
            if (s_carried != null) s_carried.DoDrop();

            IsCarried = true;
            s_carried = this;
            _carrier  = _nearbyPlayer ?? _player;

            foreach (var c in GetComponents<Collider2D>())
                if (!c.isTrigger) c.enabled = false;

            var shadow = transform.Find("_Shadow");
            if (shadow != null) shadow.gameObject.SetActive(false);

            ShowPanel();
        }

        private void DoDrop()
        {
            if (_carrier != null)
                transform.position = (Vector2)_carrier.transform.position + _carrier.LastMoveDir * holdOffset;

            IsCarried = false;
            s_carried = null;
            _carrier  = null;

            foreach (var c in GetComponents<Collider2D>())
                if (!c.isTrigger) c.enabled = true;

            var shadow = transform.Find("_Shadow");
            if (shadow != null) shadow.gameObject.SetActive(true);

            ShowPanel();
        }

        // ── 밀기 / 당기기 ────────────────────────────────────────────────────────

        private void DoPushAction()
        {
            if (_isSliding || _nearbyPlayer == null) return;
            Vector2 dir = GetSnapDir(_nearbyPlayer, fromPlayer: false);
            if (!IsMoveBlocked(dir, pushDistance))
                StartCoroutine(SlideObject(_nearbyPlayer, dir, pushDistance, pushSpeed));
        }

        private void DoPullAction()
        {
            if (_isSliding || _nearbyPlayer == null) return;
            Vector2 dir = GetSnapDir(_nearbyPlayer, fromPlayer: true);
            float distToPlayer = Vector2.Distance(transform.position, _nearbyPlayer.transform.position);
            float clampedDist  = Mathf.Min(pullDistance, Mathf.Max(0f, distToPlayer - holdOffset));
            if (clampedDist <= 0.01f) return;
            if (!IsMoveBlocked(dir, clampedDist))
                StartCoroutine(SlideObject(_nearbyPlayer, dir, clampedDist, pullSpeed));
        }

        private Vector2 GetSnapDir(PlayerController player, bool fromPlayer)
        {
            Vector2 d  = (Vector2)player.transform.position - (Vector2)transform.position;
            float   ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
            Vector2 toward = ax >= ay
                ? (d.x > 0 ? Vector2.right : Vector2.left)
                : (d.y > 0 ? Vector2.up    : Vector2.down);
            return fromPlayer ? toward : -toward;
        }

        private IEnumerator SlideObject(PlayerController player, Vector2 dir, float distance, float speed)
        {
            _isSliding      = true;
            player.IsLocked = true;
            InteractionPanel.Instance?.Hide();
            if (dirArrow != null) dirArrow.gameObject.SetActive(false);

            Vector2 start    = transform.position;
            Vector2 end      = start + dir * distance;
            float   elapsed  = 0f;
            float   duration = distance / speed;

            while (elapsed < duration)
            {
                elapsed           += Time.deltaTime;
                float smooth       = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 2f);
                transform.position = Vector2.Lerp(start, end, smooth);
                yield return null;
            }

            transform.position = end;
            player.IsLocked    = false;
            _isSliding         = false;
            _playerNearby      = false;
        }

        private bool IsMoveBlocked(Vector2 dir, float distance)
        {
            foreach (var hit in Physics2D.RaycastAll(transform.position, dir, distance))
            {
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.CompareTag("wall") || hit.collider.CompareTag("Door")) return true;
            }
            return false;
        }

        // ── 문 ───────────────────────────────────────────────────────────────────

        private void DoToggleDoor()
        {
            ApplyDoorState(!_isOpen);
            ShowPanel();
        }

        private void ApplyDoorState(bool open)
        {
            _isOpen = open;
            if (doorCollider != null) doorCollider.enabled = !open;
            if (doorRenderer != null)
            {
                if (openSprite != null && closedSprite != null)
                { doorRenderer.enabled = true; doorRenderer.sprite = open ? openSprite : closedSprite; }
                else
                    doorRenderer.enabled = !open;
            }
        }

        // ── 켜기/끄기 ────────────────────────────────────────────────────────────

        private void DoToggleLight()
        {
            _lightsOn = !_lightsOn;
            if (controlledSources != null)
                foreach (var src in controlledSources)
                    if (src != null) src.gameObject.SetActive(_lightsOn);

            ShowPanel();
        }

        // ── 인벤토리에 줍기 ──────────────────────────────────────────────────────

        private void DoAddToInventory()
        {
            if (InventoryManager.Instance == null) { Debug.LogWarning("[InteractableObject] InventoryManager 없음"); return; }
            var    sprite = _sr != null ? _sr.sprite : null;
            string name   = string.IsNullOrEmpty(itemName) ? gameObject.name : itemName;
            if (InventoryManager.Instance.TryAddItem(sprite, name, this))
            {
                HidePanel();
                SetHighlight(false);
                _playerNearby = false;
                gameObject.SetActive(false);
            }
            else
                Debug.Log("[InteractableObject] 인벤토리가 가득 찼습니다");
        }

        public void SetupAsDroppedPickup(string droppedItemName)
        {
            canInventory = true;
            itemName     = droppedItemName;
        }

        // ── 목표 대화 ────────────────────────────────────────────────────────────

        private void DoTarget()
        {
            HidePanel();
            if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsComplete) return;

            if (dialogue != null && DialogueSystem.Instance != null)
                DialogueSystem.Instance.StartDialogue(dialogue, () =>
                {
                    ObjectiveManager.Instance?.Complete();
                    ObjectiveManager.Instance?.TriggerVictory();
                });
            else
            {
                ObjectiveManager.Instance?.Complete();
                ObjectiveManager.Instance?.TriggerVictory();
            }
        }

        // ── Gizmo ────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = _playerNearby
                ? new Color(1f, 0.9f, 0.2f, 0.35f)
                : new Color(0.4f, 0.8f, 1f, 0.22f);
            Gizmos.DrawWireSphere(transform.position, approachRadius);
        }
    }
}
