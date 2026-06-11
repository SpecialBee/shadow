using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace ShadowSeller.Core
{
    // 런타임 미니맵 시스템.
    //   - Tilemap 범위를 자동 감지해 전체 맵을 덮는 Orthographic 카메라 생성 → RenderTexture → RawImage
    //   - 플레이어/Guard/Civilian 위치를 UI 아이콘으로 오버레이
    //   - Guard: 일반=붉은색, Alert/Chase=밝은 빨강   Civilian: 파란색   Player: 흰색
    //   - MiniMapPanel을 Inspector에 연결하거나, 없으면 "MiniMapPanel" 이름으로 자동 탐색
    [DefaultExecutionOrder(100)]
    public class MinimapController : MonoBehaviour
    {
        [Header("레퍼런스 (비워두면 'MiniMapPanel' 이름으로 자동 탐색)")]
        [SerializeField] private RectTransform minimapPanel;

        [Header("렌더 설정")]
        [SerializeField] private int   rtWidth       = 512;   // 렌더텍스처 가로 (세로는 패널 비율로 자동)
        [SerializeField] private float borderPadding = 4f;    // 맵 경계 여백 (월드 단위)

        [Header("아이콘")]
        [SerializeField] private float playerIconSize = 12f;
        [SerializeField] private float npcIconSize    = 9f;

        [Header("색상")]
        [SerializeField] private Color playerColor    = Color.white;
        [SerializeField] private Color guardColor     = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color guardHotColor  = new Color(1f, 0.05f, 0.05f);
        [SerializeField] private Color civilianColor  = new Color(0.4f, 0.85f, 1f);

        // ── 런타임 필드 ────────────────────────────────────────────────────────

        private Camera        _cam;
        private RenderTexture _rt;
        private RectTransform _iconRoot;

        private Transform     _playerTr;
        private RectTransform _playerIcon;

        private readonly List<(NPCController npc, RectTransform rt, Image img)> _npcs = new();

        private static Sprite _dotSprite;

        // ── 초기화 ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            AutoFindPanel();
            BuildCamera();
            BuildUI();
        }

        private void Start()
        {
            FitCameraToMap();
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

        private void BuildCamera()
        {
            var go = new GameObject("_MinimapCam");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, 0f, -100f);

            _cam                 = go.AddComponent<Camera>();
            _cam.orthographic    = true;
            _cam.orthographicSize = 50f;        // FitCameraToMap()에서 재설정
            _cam.clearFlags      = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            _cam.cullingMask     = ~LayerMask.GetMask("UI");
            _cam.depth           = -2;
        }

        private void BuildUI()
        {
            if (minimapPanel == null) return;

            // 패널 비율에 맞춰 RenderTexture 생성
            Rect panel = minimapPanel.rect;
            float aspect = panel.width / Mathf.Max(panel.height, 1f);
            int   rtH   = Mathf.Max(1, Mathf.RoundToInt(rtWidth / aspect));
            _rt          = new RenderTexture(rtWidth, rtH, 16);
            _cam.targetTexture = _rt;

            // RawImage — 지도 화면
            var viewGO          = new GameObject("_MapView", typeof(RectTransform));
            viewGO.transform.SetParent(minimapPanel, false);
            viewGO.transform.SetAsFirstSibling();
            var raw             = viewGO.AddComponent<RawImage>();
            raw.texture         = _rt;
            var rawRT           = viewGO.GetComponent<RectTransform>();
            rawRT.anchorMin     = Vector2.zero;
            rawRT.anchorMax     = Vector2.one;
            rawRT.offsetMin     = Vector2.zero;
            rawRT.offsetMax     = Vector2.zero;

            // 아이콘 컨테이너 (RawImage 위, 텍스트 라벨 아래)
            var iconGO          = new GameObject("_IconRoot", typeof(RectTransform));
            iconGO.transform.SetParent(minimapPanel, false);
            iconGO.transform.SetSiblingIndex(1);
            _iconRoot           = iconGO.GetComponent<RectTransform>();
            _iconRoot.anchorMin = Vector2.zero;
            _iconRoot.anchorMax = Vector2.one;
            _iconRoot.offsetMin = Vector2.zero;
            _iconRoot.offsetMax = Vector2.zero;

            // 패널 경계 밖으로 아이콘 삐져나오지 않도록 마스크
            if (minimapPanel.GetComponent<RectMask2D>() == null)
                minimapPanel.gameObject.AddComponent<RectMask2D>();
        }

        // ── 카메라를 맵 전체에 맞게 조정 ──────────────────────────────────────

        private void FitCameraToMap()
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

            float aspect = minimapPanel != null
                ? minimapPanel.rect.width / Mathf.Max(minimapPanel.rect.height, 1f)
                : 1f;

            // 맵 전체가 들어오도록 orthographicSize 결정
            float sizeByH = bounds.extents.y;
            float sizeByW = bounds.extents.x / aspect;
            float size    = Mathf.Max(sizeByH, sizeByW);

            _cam.aspect           = aspect;
            _cam.orthographicSize = size;
            _cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
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
                Color c  = npc.NpcKind == NpcType.Guard ? guardColor : civilianColor;
                var   rt = CreateDot(c, npcIconSize);
                var   img = rt.GetComponent<Image>();
                _npcs.Add((npc, rt, img));
            }
        }

        // ── 매 프레임 갱신 ────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_playerTr != null && _playerIcon != null)
                _playerIcon.anchoredPosition = WorldToPanel(_playerTr.position);

            foreach (var (npc, rt, img) in _npcs)
            {
                if (npc == null || !npc.gameObject.activeSelf)
                {
                    rt.gameObject.SetActive(false);
                    continue;
                }
                rt.gameObject.SetActive(true);
                rt.anchoredPosition = WorldToPanel(npc.transform.position);

                // Guard 상태에 따라 아이콘 색 변화
                if (img != null && npc.NpcKind == NpcType.Guard)
                {
                    bool hot = npc.CurrentState == NpcState.Alert
                            || npc.CurrentState == NpcState.Chase;
                    img.color = hot ? guardHotColor : guardColor;
                }
            }
        }

        // ── 좌표 변환 ─────────────────────────────────────────────────────────

        // 월드 좌표 → 미니맵 패널 내 anchoredPosition (패널 중심 기준)
        private Vector2 WorldToPanel(Vector3 worldPos)
        {
            Vector3 vp   = _cam.WorldToViewportPoint(worldPos);
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
                float alpha = Mathf.Clamp01((c - 1f - dist) * 2f + 1f); // 외곽 1px 앤티앨리어싱
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);
        }

        private void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
        }
    }
}
