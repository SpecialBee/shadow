using System.Collections;
using UnityEngine;

namespace ShadowSeller.Core
{
    // 샹들리에: 흔들림(D) ↔ 정지(B). 정지 후 stillDuration초 뒤 자동 흔들림 복귀.
    public class ChandelierObject : MonoBehaviour, IUsable
    {
        [SerializeField] private ShadowZone shadowZone;
        [SerializeField] private float      detectionRadius = 2f;
        [SerializeField] private float      stillDuration   = 4f;

        private bool      _isSwinging = true;
        private Coroutine _resetRoutine;

        public string UseHint => _isSwinging ? "E 멈추기" : "E 흔들기";

        private void Awake()
        {
            if (shadowZone != null) shadowZone.SetGrade(ExposureState.ShadowD);
        }

        public void OnUse(PlayerController user)
        {
            if (_isSwinging)
                SetStill();
            else
                SetSwinging();
        }

        private void SetStill()
        {
            _isSwinging = false;
            if (shadowZone != null) shadowZone.SetGrade(ExposureState.ShadowB);
            if (_resetRoutine != null) StopCoroutine(_resetRoutine);
            _resetRoutine = StartCoroutine(AutoReset());
        }

        private void SetSwinging()
        {
            _isSwinging = true;
            if (_resetRoutine != null) { StopCoroutine(_resetRoutine); _resetRoutine = null; }
            if (shadowZone != null) shadowZone.SetGrade(ExposureState.ShadowD);
        }

        private IEnumerator AutoReset()
        {
            yield return new WaitForSeconds(stillDuration);
            SetSwinging();
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
            Gizmos.color = _isSwinging
                ? new Color(1f, 0.7f, 0f, 0.25f)
                : new Color(0.3f, 0.6f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
