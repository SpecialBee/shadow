using UnityEngine;

namespace ShadowSeller.Core
{
    // BoxCollider2D(isTrigger=true) 필요. 밝은 구역에 배치.
    public class DangerZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var tracker = other.GetComponent<PlayerExposureTracker>();
            if (tracker != null) tracker.OnEnterDangerZone();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var tracker = other.GetComponent<PlayerExposureTracker>();
            if (tracker != null) tracker.OnExitDangerZone();
        }
    }
}
