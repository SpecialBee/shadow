using UnityEngine;
using UnityEngine.UI;

namespace ShadowSeller.UI
{
    public class HUDToggle : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;

        private bool _open = true;

        private void Awake()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(Toggle);
        }

        private void Toggle()
        {
            _open = !_open;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject != toggleButton.gameObject)
                    child.gameObject.SetActive(_open);
            }

            if (!_open)
                InteractionPanel.Instance?.Hide();
        }
    }
}
