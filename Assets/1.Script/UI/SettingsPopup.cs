using UnityEngine;
using UnityEngine.UI;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 설정 팝업 — BGM/SFX 볼륨 슬라이더, 전체화면 토글, X 닫기 버튼.
    public class SettingsPopup : MonoBehaviour
    {
        [Header("UI 연결")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Slider     bgmSlider;
        [SerializeField] private Slider     sfxSlider;
        [SerializeField] private Toggle     fullscreenToggle;
        [SerializeField] private Button     closeButton;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Close);
            bgmSlider?.onValueChanged.AddListener(OnBGMChanged);
            sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);

            if (popupRoot != null) popupRoot.SetActive(false);
        }

        public void Open()
        {
            if (popupRoot != null) popupRoot.SetActive(true);

            var am = AudioManager.Instance;
            if (am == null) return;

            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(am.BGMVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.SFXVolume);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        }

        public void Close()
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        private void OnBGMChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.BGMVolume = value;
        }

        private void OnSFXChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = value;
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
    }
}
