using UnityEngine;

namespace ShadowSeller.Core
{
    public class CarryableObject : MonoBehaviour, IInteractable
    {
        public bool IsCarried { get; private set; }

        private Collider2D _col;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pi = other.GetComponent<PlayerInteraction>();
            if (pi != null) pi.SetNearby(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pi = other.GetComponent<PlayerInteraction>();
            if (pi != null) pi.ClearNearby(this);
        }

        public void OnPickup(Transform carrier)
        {
            IsCarried = true;
            transform.SetParent(carrier);
            transform.localPosition = new Vector3(0.7f, 0f, 0f);
            if (_col != null) _col.enabled = false;
        }

        public void OnDrop()
        {
            IsCarried = false;
            transform.SetParent(null);
            if (_col != null) _col.enabled = true;
        }
    }
}
