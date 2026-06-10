using UnityEngine;

namespace ShadowSeller.Core
{
    // 인벤토리 데이터 관리 싱글턴.
    // 최대 10개 슬롯. TryAddItem으로 아이템 추가, OnItemAdded 이벤트로 UI에 통지.
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public const int MaxSlots = 10;

        public struct ItemData
        {
            public Sprite             sprite;
            public string             itemName;
            public InteractableObject sourceObject;
        }

        public static event System.Action<int, ItemData> OnItemAdded;
        public static event System.Action<int>          OnItemRemoved;

        private readonly ItemData?[] _slots = new ItemData?[MaxSlots];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryAddItem(Sprite sprite, string name, InteractableObject source = null)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != null) continue;
                var item = new ItemData { sprite = sprite, itemName = name, sourceObject = source };
                _slots[i] = item;
                OnItemAdded?.Invoke(i, item);
                return true;
            }
            return false;
        }

        public ItemData? GetSlot(int index) =>
            index >= 0 && index < MaxSlots ? _slots[index] : null;

        public ItemData? RemoveItem(int index)
        {
            if (index < 0 || index >= MaxSlots || _slots[index] == null) return null;
            var item = _slots[index].Value;
            _slots[index] = null;
            OnItemRemoved?.Invoke(index);
            return item;
        }
    }
}
