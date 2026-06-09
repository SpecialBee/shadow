using UnityEngine;

namespace ShadowSeller.Core
{
    // 커튼: E키로 펼치기/접기. 펼친 동안 ShadowZone(A) 활성.
    public class CurtainObject : MonoBehaviour, IUsable
    {
        [SerializeField] private ShadowZone shadowZone;
        [SerializeField] private float      detectionRadius = 1.5f;

        private bool _isOpen = false;

        public string UseHint => _isOpen ? "E 접기" : "E 펼치기";

        private void Awake()
        {
            if (shadowZone != null) shadowZone.gameObject.SetActive(false);
        }

        public void OnUse(PlayerController user)
        {
            _isOpen = !_isOpen;
            if (shadowZone != null) shadowZone.gameObject.SetActive(_isOpen);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.SetNearbyUsable(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            other.GetComponent<PlayerInteraction>()?.ClearNearbyUsable(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
