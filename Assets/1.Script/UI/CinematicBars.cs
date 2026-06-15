using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 컷씬용 레터박스 연출 + HUD 토글 싱글톤.
    // CutsceneDirector가 자동 호출. 씬에 빈 오브젝트에 붙여두면 됨.
    public class CinematicBars : MonoBehaviour
    {
        public static CinematicBars Instance { get; private set; }

        [Header("레터박스")]
        [Tooltip("화면 높이 대비 바 비율 (0.12 = 12%)")]
        [SerializeField] private float barHeightRatio = 0.12f;
        [SerializeField] private float animDuration   = 0.45f;

        [Header("숨길 HUD 오브젝트 (대화창 제외)")]
        [SerializeField] private GameObject[] hudObjects;

        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private float         _targetHeight;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildBars();
        }

        private void BuildBars()
        {
            var canvasGO = new GameObject("_CinematicBarsCanvas");
            canvasGO.transform.SetParent(transform);

            var canvas          = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;

            var scaler          = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode  = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor  = 1f;

            _topBar    = CreateBar(canvasGO.transform, top: true);
            _bottomBar = CreateBar(canvasGO.transform, top: false);
        }

        private static RectTransform CreateBar(Transform parent, bool top)
        {
            var go  = new GameObject(top ? "_TopBar" : "_BottomBar");
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();

            if (top)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero; // 초기에는 높이 0
            return rt;
        }

        // 컷씬 시작 — 바 슬라이드인 + HUD 숨김
        public IEnumerator Enter()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.CutsceneLetterbox);
            _targetHeight = Screen.height * barHeightRatio;
            SetHUD(false);

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                SetHeight(_targetHeight * t);
                yield return null;
            }
            SetHeight(_targetHeight);
        }

        // 컷씬 종료 — 바 슬라이드아웃 + HUD 복원
        public IEnumerator Exit()
        {
            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                SetHeight(_targetHeight * (1f - t));
                yield return null;
            }
            SetHeight(0f);
            SetHUD(true);
        }

        private void SetHeight(float px)
        {
            if (_topBar    != null) _topBar.sizeDelta    = new Vector2(0f, px);
            if (_bottomBar != null) _bottomBar.sizeDelta = new Vector2(0f, px);
        }

        private void SetHUD(bool visible)
        {
            if (hudObjects == null) return;
            foreach (var go in hudObjects)
                if (go != null) go.SetActive(visible);
        }
    }
}
