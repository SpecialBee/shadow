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

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DeactivateAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(List<(InteractionType type, string label, System.Action callback)> actions)
        {
            DeactivateAll();
            if (actions == null || actions.Count == 0) return;

            foreach (var (type, label, cb) in actions)
            {
                var btn = GetBtn(type);
                if (btn == null) continue;

                btn.gameObject.SetActive(true);

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;

                btn.onClick.RemoveAllListeners();
                var capturedCb = cb;
                btn.onClick.AddListener(() => capturedCb?.Invoke());
            }

            IsVisible = true;
        }

        public void Hide()
        {
            DeactivateAll();
            IsVisible = false;
        }

        private void DeactivateAll()
        {
            foreach (var btn in AllBtns())
                if (btn != null) btn.gameObject.SetActive(false);
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
            yield return carryBtn;
            yield return pushBtn;
            yield return pullBtn;
            yield return doorBtn;
            yield return lightBtn;
            yield return pickupBtn;
            yield return talkBtn;
        }
    }
}
