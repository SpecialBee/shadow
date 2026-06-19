using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PrologueDirector 와 공유하는 데이터 — namespace 밖에 둬야 양쪽에서 쓸 수 있음
[System.Serializable]
public class TutorialEntry
{
    public string keyLabel;
    public string description;
}

namespace ShadowSeller.UI
{
    public class TutorialPopup : MonoBehaviour
    {
        public static TutorialPopup Instance { get; private set; }

        [Header("UI 연결 — 공통")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private TMP_Text   titleText;
        [SerializeField] private Button     confirmBtn;

        [Header("텍스트 모드 (항목 리스트)")]
        [SerializeField] private TMP_Text contentText;

        [Header("이미지 모드 (그림 1장)")]
        [SerializeField] private Image contentImage;

        private System.Action                    _onClose;
        private ShadowSeller.Core.PlayerController _player;
        private bool                             _wasPlayerLocked;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            _player = Object.FindAnyObjectByType<ShadowSeller.Core.PlayerController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 텍스트 모드: 항목 리스트를 contentText에 출력
        public void Show(string title, TutorialEntry[] entries, System.Action onClose = null)
        {
            // title이 기본값("조작법 안내")이거나 비어있으면 씬에 직접 설정한 titleText 유지
            if (titleText != null && !string.IsNullOrEmpty(title) && title != "조작법 안내")
                titleText.text = title;

            if (contentText != null && entries != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var e in entries)
                    sb.AppendLine($"<b>[{e.keyLabel}]</b>  {e.description}");
                contentText.text = sb.ToString().TrimEnd();
            }

            Open(onClose);
        }

        // 이미지 모드: 스프라이트를 contentImage에 표시
        public void Show(Sprite sprite, System.Action onClose = null)
        {
            if (contentImage != null && sprite != null)
            {
                contentImage.sprite         = sprite;
                contentImage.preserveAspect = true;
            }

            Open(onClose);
        }

        private void Open(System.Action onClose)
        {
            _onClose = onClose;

            // 비활성 상태로 시작한 경우 Start()가 늦게 호출되므로 여기서 재탐색
            if (_player == null)
                _player = Object.FindAnyObjectByType<ShadowSeller.Core.PlayerController>();

            // 팝업 표시 중 플레이어 조작 잠금 (이전 상태 보존)
            if (_player != null)
            {
                _wasPlayerLocked = _player.IsLocked;
                _player.IsLocked = true;
            }

            gameObject.SetActive(true);
            if (overlay != null) overlay.SetActive(true);
            ShadowSeller.Core.AudioManager.Instance?.PlaySFX(ShadowSeller.Core.SFXClip.TutorialOpen);

            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(Close);
            }
        }

        public void Close()
        {
            // 팝업 닫힐 때 플레이어 잠금 상태 복원
            if (_player != null)
                _player.IsLocked = _wasPlayerLocked;

            if (overlay != null) overlay.SetActive(false);
            gameObject.SetActive(false);
            var cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }

    }
}
