using UnityEngine;
using TMPro;

namespace ShadowSeller.Core
{
    public class PlayerInteraction : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.Interaction;

        [SerializeField] private TMP_FontAsset hintFont;

        private InputReader _input;
        private IInteractable _nearbyCarry;
        private IUsable       _nearbyUsable;
        private BringObject   _brought;
        private GameObject    _hintFGo;
        private TextMeshPro   _hintFTmp;
        private GameObject    _hintEGo;
        private TextMeshPro   _hintETmp;

        private void Awake()
        {
            _input = GetComponent<InputReader>();

#if UNITY_EDITOR
            if (hintFont == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("DungGeunMo SDF t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    hintFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }
#endif
            BuildHints();
            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        private void BuildHints()
        {
            _hintFGo  = BuildLabel("HintF", "F 들기", 0.85f);
            _hintFTmp = _hintFGo.GetComponent<TextMeshPro>();
            _hintEGo  = BuildLabel("HintE", "E 상호작용", 1.4f);
            _hintETmp = _hintEGo.GetComponent<TextMeshPro>();
        }

        private GameObject BuildLabel(string name, string text, float yOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, yOffset, -0.1f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text               = text;
            tmp.fontSize           = 3f;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            tmp.sortingOrder       = 10;
            if (hintFont != null) tmp.font = hintFont;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(3f, 1f);

            go.SetActive(false);
            return go;
        }

        // ── BringObject (F키) 등록 ────────────────────────────────────────────

        public void SetNearby(IInteractable interactable)
        {
            _nearbyCarry = interactable;
            RefreshFHint();
        }

        public void ClearNearby(IInteractable interactable)
        {
            if (_nearbyCarry == interactable) { _nearbyCarry = null; RefreshFHint(); }
        }

        // ── IUsable (E키) 등록 ────────────────────────────────────────────────

        public void SetNearbyUsable(IUsable usable)
        {
            _nearbyUsable = usable;
            RefreshEHint();
        }

        public void ClearNearbyUsable(IUsable usable)
        {
            if (_nearbyUsable == usable) { _nearbyUsable = null; RefreshEHint(); }
        }

        // ── 힌트 갱신 ─────────────────────────────────────────────────────────

        private void RefreshFHint()
        {
            bool show = _brought != null || _nearbyCarry != null;
            _hintFTmp.text = _brought != null ? "F 놓기" : "F 들기";
            _hintFGo.SetActive(show);
        }

        private void RefreshEHint()
        {
            // BringObject 들고 있는 동안 E 힌트 숨김
            bool show = _nearbyUsable != null && _brought == null;
            if (show) _hintETmp.text = _nearbyUsable.UseHint;
            _hintEGo.SetActive(show);
        }

        // ── 틱 ───────────────────────────────────────────────────────────────

        public void Tick()
        {
            // F키 — 들기/놓기
            if (_input.PickupPressed)
            {
                if (_brought != null)
                {
                    _brought.OnDrop();
                    _brought = null;
                }
                else if (_nearbyCarry is BringObject bring)
                {
                    bring.OnPickup(transform);
                    _brought = bring;
                }
                RefreshFHint();
                RefreshEHint();
            }

            // E키 — 오브젝트 상호작용 (BringObject 들고 있으면 막힘)
            if (_input.InteractPressed && _nearbyUsable != null && _brought == null)
            {
                var player = GetComponent<PlayerController>();
                _nearbyUsable.OnUse(player);
                RefreshEHint();
            }
        }
    }
}
