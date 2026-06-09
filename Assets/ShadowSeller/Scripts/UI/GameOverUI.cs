using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ShadowSeller.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject  defeatPanel;
        [SerializeField] private GameObject  victoryPanel;
        [SerializeField] private Button      defeatRestartBtn;
        [SerializeField] private Button      victoryRestartBtn;

        private void Awake()
        {
            defeatPanel?.SetActive(false);
            victoryPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            ShadowSeller.Core.SuspicionManager.OnGameOver        += ShowDefeat;
            ShadowSeller.Core.ObjectiveManager.OnVictory         += ShowVictory;
        }

        private void OnDisable()
        {
            ShadowSeller.Core.SuspicionManager.OnGameOver        -= ShowDefeat;
            ShadowSeller.Core.ObjectiveManager.OnVictory         -= ShowVictory;
        }

        private void Start()
        {
            defeatRestartBtn?.onClick.AddListener(Restart);
            victoryRestartBtn?.onClick.AddListener(Restart);
        }

        private void ShowDefeat()
        {
            defeatPanel?.SetActive(true);
            LockInput();
        }

        private void ShowVictory()
        {
            victoryPanel?.SetActive(true);
            LockInput();
        }

        private void LockInput()
        {
            var reader = FindAnyObjectByType<ShadowSeller.Core.InputReader>();
            if (reader != null) reader.enabled = false;
        }

        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
