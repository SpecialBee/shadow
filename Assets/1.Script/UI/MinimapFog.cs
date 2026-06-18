using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowSeller.UI
{
    // 미니맵 전장의 안개 (Fog of War).
    //   플레이어가 이동하며 탐색한 영역만 미니맵에 공개.
    //   MinimapController와 같은 GameObject에 부착하거나,
    //   MinimapController를 Inspector에서 연결하세요.
    [DefaultExecutionOrder(101)]
    public class MinimapFog : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ShadowSeller.Core.MinimapController minimapController;

        [Header("탐색 설정")]
        [Tooltip("플레이어 주변 공개 반경 (월드 단위)")]
        [SerializeField] private float revealRadius = 5f;
        [Tooltip("방문 셀 하나의 크기 (작을수록 정밀하지만 메모리 증가)")]
        [SerializeField] private float cellSize = 0.5f;

        [Header("안개 설정")]
        [SerializeField] private int   fogResolution = 256;
        [SerializeField] private Color fogColor      = new Color(0.04f, 0.04f, 0.06f, 0.90f);

        // ── 런타임 ──────────────────────────────────────────────────────────
        private Camera        _cam;
        private RectTransform _panel;
        private Transform     _playerTr;

        private Texture2D _fogTex;
        private Color32[] _fogPixels;
        private Color32   _fogCol32;
        private Color32   _transparent;

        private readonly HashSet<Vector2Int> _visited = new();

        private Vector3 _lastCamPos   = Vector3.positiveInfinity;
        private float   _lastCamSize  = -1f;
        private bool    _dirty        = true;

        // ── 초기화 ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (minimapController == null)
                minimapController = FindAnyObjectByType<ShadowSeller.Core.MinimapController>();
            if (minimapController == null) { enabled = false; return; }

            _cam   = minimapController.MinimapCam;
            _panel = minimapController.MinimapPanel;

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _playerTr = playerGO.transform;

            _fogCol32   = (Color32)fogColor;
            _transparent = new Color32(0, 0, 0, 0);

            BuildFogTexture();
            BuildFogLayer();
        }

        private void BuildFogTexture()
        {
            int w = fogResolution, h = fogResolution;
            _fogTex            = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _fogTex.filterMode = FilterMode.Bilinear;
            _fogTex.wrapMode   = TextureWrapMode.Clamp;

            _fogPixels = new Color32[w * h];
            for (int i = 0; i < _fogPixels.Length; i++)
                _fogPixels[i] = _fogCol32;
            _fogTex.SetPixels32(_fogPixels);
            _fogTex.Apply(false);
        }

        private void BuildFogLayer()
        {
            var go = new GameObject("_FogLayer", typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            go.transform.SetSiblingIndex(1); // _MapView(0) 다음, _IconRoot(2) 앞

            var rt        = go.GetComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            var img             = go.AddComponent<RawImage>();
            img.texture         = _fogTex;
            img.raycastTarget   = false;
        }

        // ── 매 프레임 ────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_cam == null) return;

            if (_playerTr != null)
                MarkVisited(_playerTr.position);

            Vector3 camPos  = _cam.transform.position;
            float   camSize = _cam.orthographicSize;

            if (camPos != _lastCamPos || !Mathf.Approximately(camSize, _lastCamSize))
                _dirty = true;

            if (!_dirty) return;

            _lastCamPos  = camPos;
            _lastCamSize = camSize;
            _dirty       = false;

            RebuildFog(camPos, camSize);
        }

        // ── 방문 처리 ────────────────────────────────────────────────────────

        private void MarkVisited(Vector2 worldPos)
        {
            int cellRadius = Mathf.CeilToInt(revealRadius / cellSize);
            int cx = Mathf.RoundToInt(worldPos.x / cellSize);
            int cy = Mathf.RoundToInt(worldPos.y / cellSize);
            float rSq = (revealRadius / cellSize) * (revealRadius / cellSize);

            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                if (dx * dx + dy * dy <= rSq)
                {
                    if (_visited.Add(new Vector2Int(cx + dx, cy + dy)))
                        _dirty = true;
                }
            }
        }

        // ── 안개 텍스처 재빌드 ───────────────────────────────────────────────

        private void RebuildFog(Vector3 camPos, float camSize)
        {
            int w = _fogTex.width, h = _fogTex.height;

            float halfH = camSize;
            float halfW = camSize * _cam.aspect;

            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                float worldX = camPos.x + ((px + 0.5f) / w - 0.5f) * 2f * halfW;
                float worldY = camPos.y + ((py + 0.5f) / h - 0.5f) * 2f * halfH;

                // 3x3 이웃 셀 평균 → 자연스러운 엣지 페이드
                int revealed = 0;
                int baseCx   = Mathf.RoundToInt(worldX / cellSize);
                int baseCy   = Mathf.RoundToInt(worldY / cellSize);
                for (int ny = -1; ny <= 1; ny++)
                for (int nx = -1; nx <= 1; nx++)
                {
                    if (_visited.Contains(new Vector2Int(baseCx + nx, baseCy + ny)))
                        revealed++;
                }

                float t      = revealed / 9f;
                byte  alpha  = (byte)Mathf.RoundToInt((1f - t) * _fogCol32.a);
                _fogPixels[py * w + px] = new Color32(_fogCol32.r, _fogCol32.g, _fogCol32.b, alpha);
            }

            _fogTex.SetPixels32(_fogPixels);
            _fogTex.Apply(false);
        }

        private void OnDestroy()
        {
            if (_fogTex != null) Destroy(_fogTex);
        }
    }
}
