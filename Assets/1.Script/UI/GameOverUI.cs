using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ShadowSeller.UI
{
    // 게임 결과 UI — 패배/승리 패널 표시 및 재시작 처리.
    // SuspicionManager.OnGameOver(reason) → 패배 패널 / ObjectiveManager.OnVictory → 승리 패널.
    // 결과 표시 시 InputReader를 비활성화해 추가 입력 차단.
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject       defeatPanel;
        [SerializeField] private GameObject       victoryPanel;
        [SerializeField] private Button           defeatRestartBtn;
        [SerializeField] private Button           victoryRestartBtn;
        [SerializeField] private TextMeshProUGUI  defeatReasonText;

        private void Awake()
        {
            defeatPanel?.SetActive(false);
            victoryPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            ShadowSeller.Core.SuspicionManager.OnGameOver += ShowDefeat;
            ShadowSeller.Core.ObjectiveManager.OnVictory  += ShowVictory;
        }

        private void OnDisable()
        {
            ShadowSeller.Core.SuspicionManager.OnGameOver -= ShowDefeat;
            ShadowSeller.Core.ObjectiveManager.OnVictory  -= ShowVictory;
        }

        private void Start()
        {
            defeatRestartBtn?.onClick.AddListener(Restart);
            victoryRestartBtn?.onClick.AddListener(Restart);
        }

        private void ShowDefeat(ShadowSeller.Core.GameOverReason reason)
        {
            if (defeatReasonText != null)
            {
                defeatReasonText.text = reason switch
                {
                    ShadowSeller.Core.GameOverReason.Arrested      => "NPC에게 발각되었습니다",
                    ShadowSeller.Core.GameOverReason.SuspicionFull => "의심도가 최대에 달했습니다",
                    _                                               => string.Empty,
                };
            }
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
