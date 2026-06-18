using UnityEngine;
using UnityEngine.UI;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Image[] slotIcons = new Image[InventoryManager.MaxSlots];

        private void Start()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] == null) continue;
                slotIcons[i].raycastTarget = false;

                var slotGo = slotIcons[i].transform.parent.gameObject;
                var btn    = slotGo.GetComponent<Button>() ?? slotGo.AddComponent<Button>();
                btn.targetGraphic = slotGo.GetComponent<Image>();
                btn.interactable  = false;
            }
        }

        private void OnEnable()
        {
            InventoryManager.OnItemAdded   += HandleItemAdded;
            InventoryManager.OnItemRemoved += HandleItemRemoved;
        }

        private void OnDisable()
        {
            InventoryManager.OnItemAdded   -= HandleItemAdded;
            InventoryManager.OnItemRemoved -= HandleItemRemoved;
        }

        private void HandleItemAdded(int index, InventoryManager.ItemData data)
        {
            if (index < 0 || index >= slotIcons.Length || slotIcons[index] == null) return;
            var img = slotIcons[index];
            if (data.sprite != null) { img.sprite = data.sprite; img.color = Color.white; }
            else                     { img.color  = new Color(0.7f, 0.7f, 0.7f, 1f); }
            img.enabled = true;
        }

        private void HandleItemRemoved(int index)
        {
            if (index < 0 || index >= slotIcons.Length || slotIcons[index] == null) return;
            var img    = slotIcons[index];
            img.sprite  = null;
            img.enabled = false;
        }
    }
}
