using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowSeller.UI
{
    public class HUDToggle : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] [Range(0.1f, 0.6f)] private float animDuration = 0.25f;

        private RectTransform _rt;
        private float _openY;
        private float _closedY;
        private bool  _open = true;
        private Coroutine _anim;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (toggleButton != null)
                toggleButton.onClick.AddListener(Toggle);
        }

        private void Start()
        {
            float panelH  = _rt.rect.height;
            float buttonH = toggleButton != null
                ? toggleButton.GetComponent<RectTransform>().rect.height
                : 30f;

            _openY   = 0f;
            _closedY = -(panelH - buttonH);

            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, _openY);
        }

        private void Toggle()
        {
            _open = !_open;
            if (!_open) InteractionPanel.Instance?.Hide();

            float target = _open ? _openY : _closedY;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateTo(target));
        }

        private IEnumerator AnimateTo(float targetY)
        {
            float startY = _rt.anchoredPosition.y;
            float t = 0f;
            while (t < animDuration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / animDuration);
                float eased = 1f - (1f - ratio) * (1f - ratio) * (1f - ratio);
                _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x,
                    Mathf.Lerp(startY, targetY, eased));
                yield return null;
            }
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, targetY);
        }
    }
}
