using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    public class PausePopup : MonoBehaviour
    {
        [Header("씬")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        [Header("UI 연결")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button     btnResume;
        [SerializeField] private Button     btnMainMenu;

        private bool _isOpen;
        private bool _gameEnded;

        private void Start()
        {
            btnResume?.onClick.AddListener(Close);
            btnMainMenu?.onClick.AddListener(OnGoToMainMenu);

            if (popupRoot != null) popupRoot.SetActive(false);

            SuspicionManager.OnGameOver += _ => _gameEnded = true;
            ObjectiveManager.OnVictory  += () => _gameEnded = true;
        }

        private void OnDestroy()
        {
            SuspicionManager.OnGameOver -= _ => _gameEnded = true;
            ObjectiveManager.OnVictory  -= () => _gameEnded = true;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (_gameEnded) return;

            if (_isOpen) Close();
            else         Open();
        }

        public void Open()
        {
            if (popupRoot == null) return;
            _isOpen        = true;
            Time.timeScale = 0f;
            popupRoot.SetActive(true);
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
        }

        public void Close()
        {
            if (popupRoot == null) return;
            _isOpen        = false;
            Time.timeScale = 1f;
            popupRoot.SetActive(false);
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
        }

        private void OnGoToMainMenu()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            Time.timeScale = 1f;
            popupRoot.SetActive(false);
            StartCoroutine(GoToMainMenuRoutine());
        }

        private IEnumerator GoToMainMenuRoutine()
        {
            if (SceneFader.Instance != null)
                yield return StartCoroutine(SceneFader.Instance.FadeOut());
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
