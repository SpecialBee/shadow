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

        private System.Action _onClose;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 텍스트 모드: 항목 리스트를 contentText에 출력
        public void Show(string title, TutorialEntry[] entries, System.Action onClose = null)
        {
            if (titleText != null)
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
            gameObject.SetActive(true);           // 부모가 꺼져 있어도 반드시 활성화
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
            if (overlay != null) overlay.SetActive(false);
            gameObject.SetActive(false);          // 닫으면 다시 비활성화
            var cb = _onClose;
            _onClose = null;
            cb?.Invoke();                         // gameObject 비활성 후 콜백 실행
        }

    }
}
