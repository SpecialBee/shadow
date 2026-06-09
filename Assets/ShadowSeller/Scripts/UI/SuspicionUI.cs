using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    public class SuspicionUI : MonoBehaviour
    {
        [SerializeField] private Image          fillBar;
        [SerializeField] private TextMeshProUGUI valueLabel;

        // 구간별 색상 (설계서 §Sheet2)
        private static readonly Color ColorSafe    = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorCaution = new Color(1.0f, 0.55f, 0.0f);
        private static readonly Color ColorDanger  = new Color(0.9f, 0.15f, 0.15f);

        private void Update()
        {
            if (SuspicionManager.Instance == null) return;

            float val = SuspicionManager.Instance.CurrentSuspicion;
            float t   = val / 100f;

            if (fillBar != null)
            {
                fillBar.fillAmount = t;
                fillBar.color = val < 40f ? ColorSafe
                              : val < 70f ? ColorCaution
                                          : ColorDanger;
            }

            if (valueLabel != null)
                valueLabel.text = $"의심도  {val:F0}";
        }
    }
}
