using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace ShadowSeller.Core
{
    // 런타임 미니맵 + 전체 맵 팝업 시스템.
    //   미니맵  : 메인카메라 위치를 추적, minimapZoom 배율로 넓게 표시 (Inspector 조절 가능)
    //   팝업   : 미니맵 클릭 시 전체 맵 표시 / X버튼 또는 ESC로 닫기
    [DefaultExecutionOrder(100)]
    public class MinimapController : MonoBehaviour
    {
        [Header("레퍼런스 (비워두면 'MiniMapPanel' 이름으로 자동 탐색)")]
        [SerializeField] private RectTransform minimapPanel;

        [Header("렌더 설정")]
        [SerializeField] private int   rtWidth       = 512;
        [SerializeField] private float borderPadding = 4f;

        [Header("미니맵 줌 — 메인카메라 대비 배율 (1=동일 범위, 2=2배 넓게)")]
        [SerializeField] private float minimapZoom = 1.5f;

        [Header("아이콘")]
        [SerializeField] private float playerIconSize = 12f;
        [SerializeField] private float npcIconSize    = 9f;

        [Header("색상")]
        [SerializeField] private Color playerColor   = Color.white;
        [SerializeField] private Color guardColor    = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color guardHotColor = new Color(1f, 0.05f, 0.05f);
        [SerializeField] private Color civilianColor = new Color(0.4f, 0.85f, 1f);

        // ── 런타임 필드 ────────────────────────────────────────────────────────
        private Camera        _minimapCam;
        private Camera        _fullMapCam;
        private RenderTexture _minimapRT;
        private RenderTexture _fullMapRT;
        private RectTransform _iconRoot;
        private GameObject    _fullMapOverlay;
        private bool          _isFullMapOpen;

        private Camera        _mainCam;
        private Transform     _playerTr;
        private RectTransform _playerIcon;

        private readonly List<(NPCController npc, RectTransform rt, Image img)> _npcs = new();
        private static Sprite _dotSprite;

        // ── 초기화 ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            AutoFindPanel();
            BuildMinimapCamera();
            BuildFullMapCamera();
            BuildUI();
        }

        private void Start()
        {
            _mainCam = Camera.main;
            CalculateMapBounds();
            RegisterEntities();
        }

        private void AutoFindPanel()
        {
            if (minimapPanel != null) return;
            var go = GameObject.Find("MiniMapPanel");
            if (go != null) minimapPanel = go.GetComponent<RectTransform>();
            if (minimapPanel == null)
                Debug.LogError("[MinimapController] MiniMapPanel을 찾을 수 없습니다. Inspector에서 직접 연결하세요.");
        }

        private void BuildMinimapCamera()
        {
            var go = new GameObject("_MinimapCam");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, 0f, -100f);

            _minimapCam                  = go.AddComponent<Camera>();
            _minimapCam.orthographic     = true;
            _minimapCam.orthographicSize = 10f;
            _minimapCam.clearFlags       = CameraClearFlags.SolidColor;
            _minimapCam.backgroundColor  = new Color(0.07f, 0.07f, 0.09f);
            _minimapCam.cullingMask      = ~LayerMask.GetMask("UI");
            _minimapCam.depth            = -2;
        }

        private void BuildFullMapCamera()
        {
            var go = new GameObject("_FullMapCam");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, 0f, -100f);

            _fullMapCam                  = go.AddComponent<Camera>();
            _fullMapCam.orthographic     = true;
            _fullMapCam.orthographicSize = 50f;
            _fullMapCam.clearFlags       = CameraClearFlags.SolidColor;
            _fullMapCam.backgroundColor  = new Color(0.07f, 0.07f, 0.09f);
            _fullMapCam.cullingMask      = ~LayerMask.GetMask("UI");
            _fullMapCam.depth            = -3;
            _fullMapCam.enabled          = false; // 팝업 열 때만 활성화
        }

        private void BuildUI()
        {
            if (minimapPanel == null) return;

            // 미니맵 RT
            Rect  panel  = minimapPanel.rect;
            float aspect = panel.width / Mathf.Max(panel.height, 1f);
            int   rtH    = Mathf.Max(1, Mathf.RoundToInt(rtWidth / aspect));
            _minimapRT                = new RenderTexture(rtWidth, rtH, 16);
            _minimapCam.targetTexture = _minimapRT;
            _minimapCam.aspect        = aspect;

            // 지도 RawImage (_MapView)
            var viewGO      = new GameObject("_MapView", typeof(RectTransform));
            viewGO.transform.SetParent(minimapPanel, false);
            viewGO.transform.SetAsFirstSibling();
            var raw         = viewGO.AddComponent<RawImage>();
            raw.texture     = _minimapRT;
            var rawRT       = viewGO.GetComponent<RectTransform>();
            rawRT.anchorMin = Vector2.zero;
            rawRT.anchorMax = Vector2.one;
            rawRT.offsetMin = Vector2.zero;
            rawRT.offsetMax = Vector2.zero;

            // 클릭 → 전체 맵 팝업 (RawImage가 Graphic이므로 raycast 수신)
            var et    = viewGO.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => OpenFullMap());
            et.triggers.Add(entry);

            // 아이콘 컨테이너 (_IconRoot)
            var iconGO          = new GameObject("_IconRoot", typeof(RectTransform));
            iconGO.transform.SetParent(minimapPanel, false);
            iconGO.transform.SetSiblingIndex(1);
            _iconRoot           = iconGO.GetComponent<RectTransform>();
            _iconRoot.anchorMin = Vector2.zero;
            _iconRoot.anchorMax = Vector2.one;
            _iconRoot.offsetMin = Vector2.zero;
            _iconRoot.offsetMax = Vector2.zero;

            if (minimapPanel.GetComponent<RectMask2D>() == null)
                minimapPanel.gameObject.AddComponent<RectMask2D>();

            BuildFullMapPopup();
        }

        // ── 전체 맵 팝업 레이아웃 ──────────────────────────────────────────────

        private void BuildFullMapPopup()
        {
            var canvas = minimapPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 팝업 RT — 화면 비율로 생성
            float screenAspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
            int   popupRtH     = Mathf.Max(1, Mathf.RoundToInt(1024f / screenAspect));
            _fullMapRT                = new RenderTexture(1024, popupRtH, 16);
            _fullMapCam.targetTexture = _fullMapRT;
            _fullMapCam.aspect        = screenAspect;

            // 딤 오버레이 (전체 화면 블러 역할)
            _fullMapOverlay = new GameObject("_FullMapOverlay", typeof(RectTransform));
            _fullMapOverlay.transform.SetParent(canvas.transform, false);
            _fullMapOverlay.transform.SetAsLastSibling();
            var overlayRT       = _fullMapOverlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            var dimImg           = _fullMapOverlay.AddComponent<Image>();
            dimImg.color         = new Color(0f, 0f, 0f, 0.72f);
            dimImg.raycastTarget = true;

            // 팝업 패널 — 화면 80% 영역, 중앙 배치
            var panelGO       = new GameObject("_FullMapPanel", typeof(RectTransform));
            panelGO.transform.SetParent(_fullMapOverlay.transform, false);
            var panelRT       = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.1f, 0.08f);
            panelRT.anchorMax = new Vector2(0.9f, 0.92f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var panelBg   = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.10f, 0.97f);

            // ── 헤더 바 ──────────────────────────────────────────────────────
            const float headerH = 40f;
            const float btnW    = 44f;
            const float pad     = 8f;

            var headerGO       = new GameObject("_Header", typeof(RectTransform));
            headerGO.transform.SetParent(panelGO.transform, false);
            var headerRT       = headerGO.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot     = new Vector2(0.5f, 1f);
            headerRT.sizeDelta = new Vector2(0f, headerH);
            headerRT.offsetMax = Vector2.zero;

            var headerBg   = headerGO.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.12f, 0.16f);

            // 제목 텍스트
            var titleGO   = new GameObject("_Title", typeof(RectTransform));
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleRT   = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(pad, 0f);
            titleRT.offsetMax = new Vector2(-btnW, 0f);
            var titleTmp      = titleGO.AddComponent<TextMeshProUGUI>();
            titleTmp.text     = "전체 맵";
            titleTmp.fontSize = 16;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color    = new Color(0.9f, 0.9f, 0.9f);

            // 닫기 버튼 (헤더 우측)
            var closeGO   = new GameObject("_CloseBtn", typeof(RectTransform));
            closeGO.transform.SetParent(headerGO.transform, false);
            var closeRT   = closeGO.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot     = new Vector2(1f, 0.5f);
            closeRT.sizeDelta = new Vector2(btnW, 0f);
            closeRT.offsetMax = Vector2.zero;

            var closeBg   = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.72f, 0.15f, 0.15f);

            var closeBtn  = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            var cols = closeBtn.colors;
            cols.highlightedColor = new Color(0.92f, 0.25f, 0.25f);
            cols.pressedColor     = new Color(0.50f, 0.10f, 0.10f);
            closeBtn.colors       = cols;
            closeBtn.onClick.AddListener(CloseFullMap);

            var closeTxtGO  = new GameObject("_Txt", typeof(RectTransform));
            closeTxtGO.transform.SetParent(closeGO.transform, false);
            var closeTxtRT  = closeTxtGO.GetComponent<RectTransform>();
            closeTxtRT.anchorMin = Vector2.zero;
            closeTxtRT.anchorMax = Vector2.one;
            closeTxtRT.offsetMin = Vector2.zero;
            closeTxtRT.offsetMax = Vector2.zero;
            var closeTmp    = closeTxtGO.AddComponent<TextMeshProUGUI>();
            closeTmp.text   = "X";
            closeTmp.fontSize = 20;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color  = Color.white;

            // 전체 맵 RawImage (헤더 아래 나머지 영역 전체)
            var mapViewGO   = new GameObject("_FullMapView", typeof(RectTransform));
            mapViewGO.transform.SetParent(panelGO.transform, false);
            var mapViewRT   = mapViewGO.GetComponent<RectTransform>();
            mapViewRT.anchorMin = Vector2.zero;
            mapViewRT.anchorMax = Vector2.one;
            mapViewRT.offsetMin = new Vector2(pad, pad);
            mapViewRT.offsetMax = new Vector2(-pad, -headerH);
            var mapRaw      = mapViewGO.AddComponent<RawImage>();
            mapRaw.texture  = _fullMapRT;

            _fullMapOverlay.SetActive(false);
        }

        // ── 전체 맵 카메라: Tilemap 범위 자동 계산 ───────────────────────────

        private void CalculateMapBounds()
        {
            var tilemaps = Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            Bounds bounds = default;
            bool   first  = true;
            foreach (var tm in tilemaps)
            {
                tm.CompressBounds();
                if (tm.localBounds.size == Vector3.zero) continue;
                var wb = new Bounds(
                    tm.transform.TransformPoint(tm.localBounds.center),
                    tm.localBounds.size);
                if (first) { bounds = wb; first = false; }
                else         bounds.Encapsulate(wb);
            }
            if (first) { Debug.LogWarning("[MinimapController] Tilemap을 찾지 못했습니다."); return; }

            bounds.Expand(borderPadding * 2f);

            float popupAspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
            float sizeByH     = bounds.extents.y;
            float sizeByW     = bounds.extents.x / popupAspect;

            _fullMapCam.orthographicSize   = Mathf.Max(sizeByH, sizeByW);
            _fullMapCam.aspect             = popupAspect;
            _fullMapCam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
        }

        // ── 엔티티 등록 ───────────────────────────────────────────────────────

        private void RegisterEntities()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerTr   = playerGO.transform;
                _playerIcon = CreateDot(playerColor, playerIconSize);
            }

            var npcs = Object.FindObjectsByType<NPCController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                Color c   = npc.NpcKind == NpcType.Guard ? guardColor : civilianColor;
                var   rt  = CreateDot(c, npcIconSize);
                var   img = rt.GetComponent<Image>();
                _npcs.Add((npc, rt, img));
            }
        }

        // ── 팝업 열기 / 닫기 ──────────────────────────────────────────────────

        public void OpenFullMap()
        {
            if (_fullMapOverlay == null) return;
            _fullMapCam.enabled = true;
            _fullMapOverlay.SetActive(true);
            _isFullMapOpen = true;
        }

        public void CloseFullMap()
        {
            if (_fullMapOverlay == null) return;
            _fullMapOverlay.SetActive(false);
            _fullMapCam.enabled = false;
            _isFullMapOpen = false;
        }

        // ── 매 프레임 갱신 ────────────────────────────────────────────────────

        private void Update()
        {
            if (_isFullMapOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseFullMap();
        }

        private void LateUpdate()
        {
            // 미니맵 카메라: 메인카메라 추적 + minimapZoom 배율
            if (_mainCam != null)
            {
                Vector3 p = _mainCam.transform.position;
                _minimapCam.transform.position = new Vector3(p.x, p.y, -100f);
                _minimapCam.orthographicSize   = _mainCam.orthographicSize * minimapZoom;
            }

            // 아이콘 갱신
            if (_playerTr != null && _playerIcon != null)
                _playerIcon.anchoredPosition = WorldToPanel(_playerTr.position);

            foreach (var (npc, rt, img) in _npcs)
            {
                if (rt == null) continue;
                if (npc == null || !npc.gameObject.activeSelf)
                {
                    rt.gameObject.SetActive(false);
                    continue;
                }
                rt.gameObject.SetActive(true);
                rt.anchoredPosition = WorldToPanel(npc.transform.position);

                if (img != null && npc.NpcKind == NpcType.Guard)
                {
                    bool hot = npc.CurrentState == NpcState.Alert
                            || npc.CurrentState == NpcState.Chase;
                    img.color = hot ? guardHotColor : guardColor;
                }
            }
        }

        // ── 좌표 변환 ─────────────────────────────────────────────────────────

        private Vector2 WorldToPanel(Vector3 worldPos)
        {
            Vector3 vp   = _minimapCam.WorldToViewportPoint(worldPos);
            Vector2 size = minimapPanel.rect.size;
            return new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
        }

        // ── 아이콘 생성 ───────────────────────────────────────────────────────

        private RectTransform CreateDot(Color color, float size)
        {
            if (_dotSprite == null) _dotSprite = BuildDotSprite();

            var go     = new GameObject("_Dot", typeof(RectTransform));
            go.transform.SetParent(_iconRoot, false);

            var img    = go.AddComponent<Image>();
            img.sprite = _dotSprite;
            img.color  = color;
            img.raycastTarget = false;

            var rt        = go.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(size, size);
            return rt;
        }

        private static Sprite BuildDotSprite()
        {
            const int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = res * 0.5f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                float alpha = Mathf.Clamp01((c - 1f - dist) * 2f + 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);
        }

        private void OnDestroy()
        {
            if (_minimapRT != null) { _minimapRT.Release(); Destroy(_minimapRT); }
            if (_fullMapRT  != null) { _fullMapRT.Release();  Destroy(_fullMapRT);  }
        }
    }
}
