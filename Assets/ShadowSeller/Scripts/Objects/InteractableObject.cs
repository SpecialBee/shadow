using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShadowSeller.Core
{
    // 상호작용 가능한 오브젝트 통합 스크립트.
    // 인스펙터 체크박스로 기능 활성화 → 접근 시 하이라이트, 클릭 시 버튼 패널 표시.
    // 들기 / 밀기 / 문 열닫기 / 목표 대화 지원.
    public class InteractableObject : MonoBehaviour
    {
        // ── 상호작용 종류 (체크박스) ─────────────────────────────────────────────
        [Header("상호작용 종류")]
        [SerializeField] private bool canCarry;
        [SerializeField] private bool canPush;
        [SerializeField] private bool isDoor;
        [SerializeField] private bool isTarget;

        // ── 들기 설정 ────────────────────────────────────────────────────────────
        [Header("들기 설정")]
        [SerializeField] private float holdOffset = 0.7f;

        // ── 밀기 설정 ────────────────────────────────────────────────────────────
        [Header("밀기 설정")]
        [SerializeField] private float pushDistance = 2f;
        [SerializeField] private float slideSpeed   = 4f;

        // ── 문 설정 ──────────────────────────────────────────────────────────────
        [Header("문 설정")]
        [SerializeField] private Collider2D     doorCollider;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite         openSprite;
        [SerializeField] private Sprite         closedSprite;
        [SerializeField] private bool           startOpen;

        // ── 접근 강조 설정 ───────────────────────────────────────────────────────
        [Header("접근 강조")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField][Range(0f, 1f)] private float highlightAlpha = 1f;
        [SerializeField] private float approachRadius = 2f;
        [SerializeField] private float buttonYOffset  = 1.2f;

        // ── 공개 상태 ─────────────────────────────────────────────────────────────
        public bool IsCarried { get; private set; }

        // ── 내부 상태 ─────────────────────────────────────────────────────────────
        private SpriteRenderer   _sr;
        private Color            _originalColor;
        private PlayerController _nearbyPlayer;
        private PlayerController _carrier;
        private bool             _playerNearby;
        private bool             _isOpen;
        private bool             _isSliding;

        // ── 버튼 패널 ─────────────────────────────────────────────────────────────
        private GameObject     _panelGo;
        private int            _panelOpenedFrame = -1;
        private TMP_FontAsset  _font;

        private readonly List<BtnData> _buttons = new List<BtnData>();

        private struct BtnData
        {
            public Vector2        LocalOffset; // transform 기준 버튼 중심 오프셋
            public Vector2        Size;
            public System.Action  Callback;
        }

        // 씬 전체 오브젝트 목록 — 다른 패널 일괄 닫기용
        private static readonly List<InteractableObject> s_all = new List<InteractableObject>();
        // 현재 들고 있는 오브젝트
        private static InteractableObject s_carried;

        // ────────────────────────────────────────────────────────────────────────
        //  초기화
        // ────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            s_all.Add(this);

            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;

            if (isDoor) ApplyDoorState(startOpen);

            // 접근 감지용 trigger 동적 추가
            var trigger       = gameObject.AddComponent<CircleCollider2D>();
            trigger.radius    = approachRadius;
            trigger.isTrigger = true;

            LoadFont();
        }

        private void OnDestroy()
        {
            s_all.Remove(this);
            if (s_carried == this)
            {
                s_carried = null;
                IsCarried = false;
            }
        }

        private void LoadFont()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("DungGeunMo SDF t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            }
#endif
        }

        // ────────────────────────────────────────────────────────────────────────
        //  접근 감지 & 하이라이트
        // ────────────────────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;
            _nearbyPlayer = player;
            _playerNearby = true;
            SetHighlight(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _nearbyPlayer = null;
            _playerNearby = false;
            SetHighlight(false);
            HidePanel();
        }

        private void SetHighlight(bool on)
        {
            if (_sr == null) return;
            _sr.color = on
                ? new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightAlpha)
                : _originalColor;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  클릭 감지
        // ────────────────────────────────────────────────────────────────────────

        private void OnMouseDown()
        {
            if (!_playerNearby && !IsCarried) return;

            if (_panelGo != null && _panelGo.activeSelf)
            {
                HidePanel();
                return;
            }

            ShowPanel();
            _panelOpenedFrame = Time.frameCount;
        }

        private void Update()
        {
            if (_panelGo == null || !_panelGo.activeSelf) return;

            bool mouseDown;
#if ENABLE_INPUT_SYSTEM
            mouseDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            mouseDown = Input.GetMouseButtonDown(0);
#endif
            if (!mouseDown) return;
            if (Time.frameCount == _panelOpenedFrame) return;

            Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mp.z = 0f;

            foreach (var btn in _buttons)
            {
                Vector2 center = (Vector2)transform.position + btn.LocalOffset;
                var     rect   = new Rect(center.x - btn.Size.x * 0.5f,
                                          center.y - btn.Size.y * 0.5f,
                                          btn.Size.x, btn.Size.y);
                if (rect.Contains((Vector2)mp))
                {
                    btn.Callback.Invoke();
                    return;
                }
            }

            HidePanel();
        }

        private void LateUpdate()
        {
            if (!IsCarried || _carrier == null) return;
            transform.position = (Vector2)_carrier.transform.position
                               + _carrier.LastMoveDir * holdOffset;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  패널 표시 / 숨김
        // ────────────────────────────────────────────────────────────────────────

        private void ShowPanel()
        {
            foreach (var obj in s_all)
                if (obj != this) obj.HidePanel();

            var actions = CollectActions();
            if (actions.Count == 0) return;

            if (_panelGo != null) Destroy(_panelGo);
            _buttons.Clear();

            _panelGo = new GameObject("_ButtonPanel");
            _panelGo.transform.SetParent(transform);
            _panelGo.transform.localScale    = Vector3.one;
            _panelGo.transform.localPosition = Vector3.zero;

            float btnW  = 1.0f;
            float btnH  = 0.42f;
            float gap   = 0.12f;
            float total = actions.Count * btnW + (actions.Count - 1) * gap;
            float startX = -total * 0.5f + btnW * 0.5f;

            for (int i = 0; i < actions.Count; i++)
            {
                var (label, cb) = actions[i];
                float lx = startX + i * (btnW + gap);
                var localPos = new Vector2(lx, buttonYOffset);
                CreateButtonGO(label, localPos, new Vector2(btnW, btnH));

                _buttons.Add(new BtnData
                {
                    LocalOffset = localPos,
                    Size        = new Vector2(btnW, btnH),
                    Callback    = cb
                });
            }
        }

        private void HidePanel()
        {
            if (_panelGo == null) return;
            Destroy(_panelGo);
            _panelGo = null;
            _buttons.Clear();
        }

        // ────────────────────────────────────────────────────────────────────────
        //  버튼 GO 생성 (SpriteRenderer 배경 + TextMeshPro 레이블)
        // ────────────────────────────────────────────────────────────────────────

        private void CreateButtonGO(string label, Vector2 localPos, Vector2 size)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(_panelGo.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = new Vector3(size.x, size.y, 1f);

            // 배경
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            var bg          = go.AddComponent<SpriteRenderer>();
            bg.sprite       = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            bg.color        = new Color(0.08f, 0.08f, 0.08f, 0.88f);
            bg.sortingOrder = 15;

            // 레이블 — 스케일 역보정하여 글자 비율 유지
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform);
            txtGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            txtGo.transform.localScale    = new Vector3(1f / size.x, 1f / size.y, 1f);

            var tmp = txtGo.AddComponent<TextMeshPro>();
            tmp.text               = label;
            tmp.fontSize           = 3f;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            tmp.sortingOrder       = 16;
            if (_font != null) tmp.font = _font;

            var rt = txtGo.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(200f, 60f);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  액션 목록 조합
        // ────────────────────────────────────────────────────────────────────────

        private List<(string, System.Action)> CollectActions()
        {
            var list = new List<(string, System.Action)>();

            if (IsCarried)
            {
                list.Add(("놓기", DoDrop));
                return list;
            }

            if (canCarry)
                list.Add(("들기", DoCarry));

            if (canPush)
                list.Add(("밀기", DoPushAction));

            if (isDoor)
            {
                string doorLabel = _isOpen ? "닫기" : "열기";
                list.Add((doorLabel, DoToggleDoor));
            }

            if (isTarget)
            {
                bool done = ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsComplete;
                list.Add((done ? "완료됨" : "대화", DoTarget));
            }

            return list;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  상호작용 구현
        // ────────────────────────────────────────────────────────────────────────

        private void DoCarry()
        {
            HidePanel();
            if (s_carried != null) s_carried.DoDrop();

            IsCarried = true;
            s_carried = this;
            _carrier  = _nearbyPlayer;

            foreach (var c in GetComponents<Collider2D>())
                if (!c.isTrigger) c.enabled = false;

            var shadow = transform.Find("_Shadow");
            if (shadow != null) shadow.gameObject.SetActive(false);
        }

        private void DoDrop()
        {
            HidePanel();
            if (_carrier != null)
                transform.position = (Vector2)_carrier.transform.position
                                   + _carrier.LastMoveDir * holdOffset;

            IsCarried = false;
            s_carried = null;
            _carrier  = null;

            foreach (var c in GetComponents<Collider2D>())
                if (!c.isTrigger) c.enabled = true;

            var shadow = transform.Find("_Shadow");
            if (shadow != null) shadow.gameObject.SetActive(true);
        }

        private void DoPushAction()
        {
            HidePanel();
            if (_isSliding || _nearbyPlayer == null) return;
            Vector2 dir = GetPushDir(_nearbyPlayer);
            if (!IsPushBlocked(dir))
                StartCoroutine(SlidePush(_nearbyPlayer, dir));
        }

        private void DoToggleDoor()
        {
            HidePanel();
            ApplyDoorState(!_isOpen);
        }

        private void DoTarget()
        {
            HidePanel();
            ObjectiveManager.Instance?.Complete();
        }

        // ────────────────────────────────────────────────────────────────────────
        //  밀기
        // ────────────────────────────────────────────────────────────────────────

        private IEnumerator SlidePush(PlayerController player, Vector2 dir)
        {
            _isSliding      = true;
            player.IsLocked = true;

            Vector2 start    = transform.position;
            Vector2 end      = start + dir * pushDistance;
            float   elapsed  = 0f;
            float   duration = pushDistance / slideSpeed;

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
        }

        private Vector2 GetPushDir(PlayerController player)
        {
            Vector2 d  = (Vector2)player.transform.position - (Vector2)transform.position;
            float   ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
            return ax >= ay
                ? (d.x > 0 ? Vector2.right : Vector2.left)
                : (d.y > 0 ? Vector2.up    : Vector2.down);
        }

        private bool IsPushBlocked(Vector2 dir)
        {
            foreach (var hit in Physics2D.RaycastAll(transform.position, dir, pushDistance))
            {
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Door")) return true;
            }
            return false;
        }

        // ────────────────────────────────────────────────────────────────────────
        //  문 상태
        // ────────────────────────────────────────────────────────────────────────

        private void ApplyDoorState(bool open)
        {
            _isOpen = open;
            if (doorCollider != null) doorCollider.enabled = !open;
            if (doorRenderer != null)
            {
                if (openSprite != null && closedSprite != null)
                {
                    doorRenderer.enabled = true;
                    doorRenderer.sprite  = open ? openSprite : closedSprite;
                }
                else
                    doorRenderer.enabled = !open;
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        //  Gizmo
        // ────────────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = _playerNearby
                ? new Color(1f, 0.9f, 0.2f, 0.35f)
                : new Color(0.4f, 0.8f, 1f, 0.22f);
            Gizmos.DrawWireSphere(transform.position, approachRadius);
        }
    }
}
