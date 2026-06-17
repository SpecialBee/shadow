using UnityEngine;
using UnityEngine.UI;

namespace ShadowSeller.UI
{
    public class ExaminePopup : MonoBehaviour
    {
        public static ExaminePopup Instance { get; private set; }

        [Header("UI 연결")]
        [SerializeField] private GameObject overlay;      // 팝업 전체 루트 (기본 비활성화)
        [SerializeField] private Image      examineImage; // 스프라이트를 표시할 Image
        [SerializeField] private Button     closeBtn;     // X 버튼

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (overlay != null) overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(Sprite sprite)
        {
            if (sprite == null) return;

            examineImage.sprite        = sprite;
            examineImage.preserveAspect = true;
            overlay.SetActive(true);

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }
        }

        public void Close()
        {
            ShadowSeller.Core.DialogueSystem.Instance?.ForceEnd();
            if (overlay != null) overlay.SetActive(false);
        }

        private void Update()
        {
            if (overlay == null || !overlay.activeSelf) return;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
        }
    }
}
