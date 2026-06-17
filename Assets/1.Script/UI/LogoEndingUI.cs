using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadowSeller.Core
{
    public class LogoEndingUI : MonoBehaviour
    {
        public static LogoEndingUI Instance { get; private set; }

        [Header("UI 연결")]
        [SerializeField] private CanvasGroup logoGroup;

        [Header("타이밍 설정")]
        [SerializeField] private float waitBeforeLogo = 0.5f;
        [SerializeField] private float logoFadeInTime = 1.5f;
        [SerializeField] private float holdTime       = 2.0f;

        [Header("씬 이동")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _logoVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (logoGroup != null) { logoGroup.alpha = 0f; logoGroup.gameObject.SetActive(false); }
        }

        private void Update()
        {
            if (!_logoVisible) return;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
                GoToMainMenu();
#else
            if (Input.GetKeyDown(KeyCode.Space))
                GoToMainMenu();
#endif
        }

        public void Play()
        {
            StartCoroutine(EndingRoutine());
        }

        private IEnumerator EndingRoutine()
        {
            HideGameWorld();

            yield return new WaitForSeconds(waitBeforeLogo);

            if (logoGroup != null)
            {
                logoGroup.gameObject.SetActive(true);
                yield return StartCoroutine(FadeGroup(logoGroup, 0f, 1f, logoFadeInTime));
            }

            _logoVisible = true;

            yield return new WaitForSeconds(holdTime);

            // holdTime 이후 자동으로 메인메뉴 이동
            GoToMainMenu();
        }

        private void GoToMainMenu()
        {
            _logoVisible = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HideGameWorld()
        {
            var follow = Camera.main?.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;

            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.IsLocked = true;
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
