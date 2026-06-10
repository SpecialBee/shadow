using UnityEngine;
using UnityEngine.UI;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Image[] slotIcons = new Image[InventoryManager.MaxSlots];

        private PlayerController _player;
        private Button[]         _slotButtons;

        private void Start()
        {
            _player      = Object.FindAnyObjectByType<PlayerController>();
            _slotButtons = new Button[slotIcons.Length];

            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] == null) continue;
                int idx    = i;
                var slotGo = slotIcons[i].transform.parent.gameObject;
                slotIcons[i].raycastTarget = false;

                var btn    = slotGo.GetComponent<Button>() ?? slotGo.AddComponent<Button>();
                btn.targetGraphic = slotGo.GetComponent<Image>();
                btn.interactable  = false;   // 아이템 없을 때는 비활성
                var colors              = btn.colors;
                colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                colors.pressedColor     = new Color(0.6f, 0.6f, 0.6f, 1f);
                btn.colors = colors;
                btn.onClick.AddListener(() => OnSlotClicked(idx));

                _slotButtons[i] = btn;
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
            if (_slotButtons != null && index < _slotButtons.Length && _slotButtons[index] != null)
                _slotButtons[index].interactable = true;
        }

        private void HandleItemRemoved(int index)
        {
            if (index < 0 || index >= slotIcons.Length || slotIcons[index] == null) return;
            var img    = slotIcons[index];
            img.sprite  = null;
            img.enabled = false;
            if (_slotButtons != null && index < _slotButtons.Length && _slotButtons[index] != null)
                _slotButtons[index].interactable = false;
        }

        private void OnSlotClicked(int index)
        {
            var item = InventoryManager.Instance?.RemoveItem(index);
            if (item == null) return;
            SpawnDroppedItem(item.Value);
        }

        private void SpawnDroppedItem(InventoryManager.ItemData item)
        {
            Vector3 dropPos = Vector3.zero;
            if (_player != null)
            {
                Vector2 dir = _player.LastMoveDir.sqrMagnitude > 0.01f
                    ? _player.LastMoveDir
                    : Vector2.down;
                dropPos = _player.transform.position + (Vector3)(dir * 1f);
            }

            if (item.sourceObject != null)
            {
                item.sourceObject.transform.position = new Vector3(dropPos.x, dropPos.y, 0f);
                item.sourceObject.gameObject.SetActive(true);
                return;
            }

            // sourceObject가 없는 경우 (비정상 경로) — 최소한의 폴백
            var go = new GameObject(item.itemName);
            go.transform.position = new Vector3(dropPos.x, dropPos.y, 0f);
            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = item.sprite;
            sr.sortingOrder = 1;
            if (item.sprite != null)
            {
                var col  = go.AddComponent<BoxCollider2D>();
                col.size = item.sprite.bounds.size;
            }
            var interactable = go.AddComponent<InteractableObject>();
            interactable.SetupAsDroppedPickup(item.itemName);
        }
    }
}
