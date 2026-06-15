using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowSeller.UI
{
    // 씬 전환용 페이드 인/아웃 — DontDestroyOnLoad 싱글톤.
    // Awake에서 전체화면 검정 Canvas를 자동 생성.
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [SerializeField] private float fadeDuration = 0.4f;

        private CanvasGroup _cg;
        private bool        _pendingFadeIn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
            _cg.alpha = 0f;
        }

        private void OnEnable()  => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (!_pendingFadeIn) return;
            _pendingFadeIn = false;
            StartCoroutine(FadeIn());
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("_FadeCanvas");
            canvasGO.transform.SetParent(transform);

            var canvas              = canvasGO.AddComponent<Canvas>();
            canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder     = 9999;

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var imgGO = new GameObject("_FadeImage");
            imgGO.transform.SetParent(canvasGO.transform, false);

            var rt         = imgGO.AddComponent<RectTransform>();
            rt.anchorMin   = Vector2.zero;
            rt.anchorMax   = Vector2.one;
            rt.offsetMin   = Vector2.zero;
            rt.offsetMax   = Vector2.zero;

            var img        = imgGO.AddComponent<Image>();
            img.color      = Color.black;
            img.raycastTarget = true;

            _cg            = canvasGO.AddComponent<CanvasGroup>();
            _cg.alpha      = 0f;
            _cg.blocksRaycasts = false;
        }

        public IEnumerator FadeOut()
        {
            _cg.blocksRaycasts = true;
            yield return Animate(0f, 1f);
            _pendingFadeIn = true;
        }

        public IEnumerator FadeIn()
        {
            yield return Animate(1f, 0f);
            _cg.blocksRaycasts = false;
        }

        private IEnumerator Animate(float from, float to)
        {
            float elapsed = 0f;
            _cg.alpha = from;
            while (elapsed < fadeDuration)
            {
                elapsed   += Time.unscaledDeltaTime;
                _cg.alpha  = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            _cg.alpha = to;
        }
    }
}
