using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 메인 메뉴 — 새 게임 / 이어하기 / 설정 / 종료.
    public class MainMenuUI : MonoBehaviour
    {
        [Header("씬 이름")]
        [SerializeField] private string prologueScene = "Prologue";

        [Header("버튼")]
        [SerializeField] private Button       btnNewGame;
        [SerializeField] private Button       btnContinue;
        [SerializeField] private Button       btnSettings;
        [SerializeField] private Button       btnQuit;

        [Header("이어하기 비활성 색상")]
        [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("설정 팝업")]
        [SerializeField] private SettingsPopup settingsPopup;

        [Header("페이드")]
        [SerializeField] private float fadeDelay = 0.15f;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(BGMTrack.MainMenu);

            btnNewGame?.onClick.AddListener(OnNewGame);
            btnContinue?.onClick.AddListener(OnContinue);
            btnSettings?.onClick.AddListener(OnSettings);
            btnQuit?.onClick.AddListener(OnQuit);

            RefreshContinueButton();
        }

        private void RefreshContinueButton()
        {
            if (btnContinue == null) return;

            bool hasSave = CheckpointManager.Instance != null
                           && CheckpointManager.Instance.HasCheckpoint;

            btnContinue.interactable = hasSave;

            // 버튼 텍스트 색상 dimmed 처리
            var text = btnContinue.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null) text.color = hasSave ? Color.white : dimColor;
        }

        // ── 버튼 핸들러 ──────────────────────────────────────────────

        private void OnNewGame()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            StartCoroutine(LoadScene(prologueScene));
        }

        private void OnContinue()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            // CheckpointManager.Respawn()은 내부에서 SceneFader + LoadScene 처리
            CheckpointManager.Instance?.Respawn();
        }

        private void OnSettings()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            settingsPopup?.Open();
        }

        private void OnQuit()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            StartCoroutine(QuitRoutine());
        }

        // ── 씬 전환 ──────────────────────────────────────────────────

        private IEnumerator LoadScene(string sceneName)
        {
            yield return new WaitForSeconds(fadeDelay);

            if (SceneFader.Instance != null)
                yield return StartCoroutine(SceneFader.Instance.FadeOut());

            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator QuitRoutine()
        {
            yield return new WaitForSeconds(fadeDelay);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
