using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 의심도 UI — SuspicionManager.CurrentSuspicion을 Slider로 표시.
    // 구간별 색상: 0~39=회색(안전) / 40~69=주황(주의) / 70~100=빨강(위험).
    // 복합 승수(B) 피드백: 동시 감시 Civilian이 2명 이상이면 Fill이 맥동함.
    public class SuspicionUI : MonoBehaviour
    {
        [SerializeField] private Slider          slider;
        [SerializeField] private TextMeshProUGUI valueLabel;

        private static readonly Color ColorSafe    = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorCaution = new Color(1.0f, 0.55f, 0.0f);
        private static readonly Color ColorDanger  = new Color(0.9f, 0.15f, 0.15f);

        private Image                _fillImage;
        private PlayerExposureTracker _tracker;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
                _tracker = playerGo.GetComponent<PlayerExposureTracker>();

            if (slider != null && slider.fillRect != null)
                _fillImage = slider.fillRect.GetComponent<Image>();
        }

        private void Update()
        {
            if (SuspicionManager.Instance == null) return;

            float val = SuspicionManager.Instance.CurrentSuspicion;

            if (slider != null)
                slider.value = val / 100f;

            if (_fillImage != null)
            {
                Color baseColor = val < 40f ? ColorSafe
                                : val < 70f ? ColorCaution
                                            : ColorDanger;

                if (_tracker != null)
                {
                    float crowd = _tracker.GetCrowdMultiplier();
                    if (crowd > 1f)
                    {
                        float freq  = 2f + (crowd - 1f) * 3f;
                        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * freq * Mathf.PI * 2f);
                        baseColor = Color.Lerp(baseColor * 0.55f, baseColor, pulse);
                    }
                }

                _fillImage.color = baseColor;
            }

            if (valueLabel != null)
                valueLabel.text = $"의심도  {val:F0}";
        }
    }
}
