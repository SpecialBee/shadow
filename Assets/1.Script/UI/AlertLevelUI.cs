using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 전역 경계 레벨 UI.
    //   HUD 아이콘: 현재 레벨을 색상으로 상시 표시.
    //   알림 텍스트: 레벨 상승 시 화면 중앙에 잠깐 표시 후 페이드아웃.
    //   화면 플래시: 레벨 상승 순간 붉은 플래시.
    public class AlertLevelUI : MonoBehaviour
    {
        [Header("HUD 아이콘 (상시)")]
        [SerializeField] private Image            levelIcon;
        [SerializeField] private TextMeshProUGUI  levelNumber;

        [Header("레벨 상승 알림 (일시)")]
        [SerializeField] private TextMeshProUGUI  notificationText;
        [SerializeField] private float            notificationDuration = 2.5f;

        [Header("화면 플래시")]
        [SerializeField] private Image flashOverlay;
        [SerializeField] private float flashDuration = 0.35f;

        private static readonly Color[] Colors =
        {
            new Color(0.80f, 0.80f, 0.80f), // 레벨 1: 흰
            new Color(1.00f, 0.85f, 0.00f), // 레벨 2: 노랑
            new Color(1.00f, 0.45f, 0.00f), // 레벨 3: 주황
            new Color(0.90f, 0.10f, 0.10f), // 레벨 4: 빨강
        };

        private static readonly string[] Labels =
            { "경계 레벨 1", "경계 레벨 2", "경계 레벨 3", "경계 레벨 4" };

        private Coroutine _notifyRoutine;

        private void OnEnable()  => AlertManager.OnAlertLevelChanged += OnLevelChanged;
        private void OnDisable() => AlertManager.OnAlertLevelChanged -= OnLevelChanged;

        private void Start()
        {
            UpdateIcon(AlertManager.Instance?.Level ?? 1);
            if (notificationText != null) notificationText.alpha = 0f;
            SetFlashAlpha(0f);
        }

        private void OnLevelChanged(int level)
        {
            UpdateIcon(level);
            if (_notifyRoutine != null) StopCoroutine(_notifyRoutine);
            _notifyRoutine = StartCoroutine(NotifyRoutine(level));
        }

        private void UpdateIcon(int level)
        {
            Color col = Colors[Mathf.Clamp(level - 1, 0, 3)];
            if (levelIcon   != null) levelIcon.color = col;
            if (levelNumber != null) { levelNumber.text = level.ToString(); levelNumber.color = col; }
        }

        private IEnumerator NotifyRoutine(int level)
        {
            Color col = Colors[Mathf.Clamp(level - 1, 0, 3)];

            // 플래시
            SetFlashAlpha(0.4f);
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                SetFlashAlpha(Mathf.Lerp(0.4f, 0f, t / flashDuration));
                yield return null;
            }
            SetFlashAlpha(0f);

            // 알림 텍스트 표시
            if (notificationText != null)
            {
                notificationText.text  = Labels[Mathf.Clamp(level - 1, 0, 3)];
                notificationText.color = col;
                notificationText.alpha = 1f;

                yield return new WaitForSeconds(Mathf.Max(0f, notificationDuration - 0.5f));

                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    notificationText.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                    yield return null;
                }
                notificationText.alpha = 0f;
            }
        }

        private void SetFlashAlpha(float a)
        {
            if (flashOverlay == null) return;
            var c = flashOverlay.color; c.a = a; flashOverlay.color = c;
        }
    }
}
