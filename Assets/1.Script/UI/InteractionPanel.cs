using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowSeller.UI
{
    public class InteractionPanel : MonoBehaviour
    {
        public static InteractionPanel Instance { get; private set; }

        [SerializeField] private RectTransform   panel;
        [SerializeField] private RectTransform   btnContainer;
        [SerializeField] private GameObject      overlay;
        [SerializeField] private TMP_FontAsset   font;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        private readonly List<GameObject> _btnGos = new List<GameObject>();

        private const float BtnH    = 38f;
        private const float Spacing = 4f;
        private const float PadY    = 10f;
        private const float PanelW  = 150f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var overlayBtn = overlay.GetComponent<Button>();
            if (overlayBtn != null) overlayBtn.onClick.AddListener(Hide);
            panel.gameObject.SetActive(false);
            overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(List<(string label, System.Action callback)> actions)
        {
            Hide();
            if (actions == null || actions.Count == 0) return;

            foreach (var (label, cb) in actions)
            {
                var capturedCb = cb;
                var btnGo = CreateButtonGO(label, () => { capturedCb.Invoke(); Hide(); }, font);
                btnGo.transform.SetParent(btnContainer, false);
                _btnGos.Add(btnGo);
            }

            float panelH = PadY + actions.Count * BtnH + (actions.Count - 1) * Spacing + PadY;
            panel.sizeDelta = new Vector2(PanelW, panelH);

            overlay.SetActive(true);
            panel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            foreach (var go in _btnGos) Destroy(go);
            _btnGos.Clear();
            if (panel != null) panel.gameObject.SetActive(false);
            if (overlay != null) overlay.SetActive(false);
        }

        private static GameObject CreateButtonGO(string label, System.Action onClick, TMP_FontAsset font = null)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors              = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f, 1f);
            colors.pressedColor     = new Color(0.45f, 0.45f, 0.45f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick.Invoke());

            go.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelW - 20f, BtnH);

            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt       = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text               = label;
            tmp.fontSize           = 14f;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            if (font != null) tmp.font = font;

            return go;
        }
    }
}
