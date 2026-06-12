using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowSeller.UI
{
    public enum InteractionType { Carry, Push, Pull, Door, Light, Pickup, Talk }

    public class InteractionPanel : MonoBehaviour
    {
        public static InteractionPanel Instance { get; private set; }

        [Header("버튼 슬롯 (Inspector에서 연결)")]
        [SerializeField] private Button carryBtn;
        [SerializeField] private Button pushBtn;
        [SerializeField] private Button pullBtn;
        [SerializeField] private Button doorBtn;
        [SerializeField] private Button lightBtn;
        [SerializeField] private Button pickupBtn;
        [SerializeField] private Button talkBtn;

        [Header("투명도")]
        [SerializeField] [Range(0f, 1f)] private float activeAlpha = 1f;
        [SerializeField] [Range(0f, 1f)] private float inactiveAlpha = 0.25f;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DimAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(List<(InteractionType type, string label, System.Action callback)> actions)
        {
            DimAll();
            if (actions == null || actions.Count == 0) return;

            foreach (var (type, label, cb) in actions)
            {
                var btn = GetBtn(type);
                if (btn == null) continue;

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;

                var captured = cb;
                SetButtonState(btn, true, captured);
            }

            IsVisible = true;
        }

        public void Hide()
        {
            DimAll();
            IsVisible = false;
        }

        // ── 내부 ────────────────────────────────────────────────────────────────

        private void DimAll()
        {
            foreach (var btn in AllBtns())
                if (btn != null) SetButtonState(btn, false, null);
            IsVisible = false;
        }

        private void SetButtonState(Button btn, bool on, System.Action callback)
        {
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

            cg.alpha          = on ? activeAlpha : inactiveAlpha;
            cg.interactable   = on;
            cg.blocksRaycasts = on;

            btn.onClick.RemoveAllListeners();
            if (on && callback != null)
            {
                var cap = callback;
                btn.onClick.AddListener(() => cap?.Invoke());
            }
        }

        private Button GetBtn(InteractionType t) => t switch
        {
            InteractionType.Carry  => carryBtn,
            InteractionType.Push   => pushBtn,
            InteractionType.Pull   => pullBtn,
            InteractionType.Door   => doorBtn,
            InteractionType.Light  => lightBtn,
            InteractionType.Pickup => pickupBtn,
            InteractionType.Talk   => talkBtn,
            _                      => null,
        };

        private IEnumerable<Button> AllBtns()
        {
            yield return carryBtn; yield return pushBtn; yield return pullBtn;
            yield return doorBtn;  yield return lightBtn; yield return pickupBtn;
            yield return talkBtn;
        }
    }
}
