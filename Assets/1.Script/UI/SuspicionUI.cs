using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 의심도 UI — SuspicionManager.CurrentSuspicion을 매 Update마다 읽어 fillBar와 텍스트 갱신.
    // 구간별 색상: 0~39=회색(안전) / 40~69=주황(주의) / 70~100=빨강(위험).
    // 복합 승수(B) 피드백: 동시 감시 Civilian이 2명 이상이면 바가 맥동함.
    public class SuspicionUI : MonoBehaviour
    {
        [SerializeField] private Image           fillBar;
        [SerializeField] private TextMeshProUGUI valueLabel;

        private static readonly Color ColorSafe    = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorCaution = new Color(1.0f, 0.55f, 0.0f);
        private static readonly Color ColorDanger  = new Color(0.9f, 0.15f, 0.15f);

        private PlayerExposureTracker _tracker;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
                _tracker = playerGo.GetComponent<PlayerExposureTracker>();
        }

        private void Update()
        {
            if (SuspicionManager.Instance == null) return;

            float val = SuspicionManager.Instance.CurrentSuspicion;
            float t   = val / 100f;

            if (fillBar != null)
            {
                fillBar.fillAmount = t;

                Color baseColor = val < 40f ? ColorSafe
                                : val < 70f ? ColorCaution
                                            : ColorDanger;

                // 복합 승수 맥동: 동시에 보는 Civilian이 많을수록 빠르게 깜빡임
                if (_tracker != null)
                {
                    float crowd = _tracker.GetCrowdMultiplier();
                    if (crowd > 1f)
                    {
                        float freq  = 2f + (crowd - 1f) * 3f; // 2~5 Hz
                        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * freq * Mathf.PI * 2f);
                        baseColor = Color.Lerp(baseColor * 0.55f, baseColor, pulse);
                    }
                }

                fillBar.color = baseColor;
            }

            if (valueLabel != null)
                valueLabel.text = $"의심도  {val:F0}";
        }
    }
}
