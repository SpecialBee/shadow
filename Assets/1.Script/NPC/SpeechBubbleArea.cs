using UnityEngine;

namespace ShadowSeller.Core
{
    [RequireComponent(typeof(SpeechBubble))]
    public class SpeechBubbleArea : MonoBehaviour
    {
        [Header("감지 범위")]
        [SerializeField] private float triggerRadius = 2f;

        [Header("말풍선 텍스트")]
        [SerializeField] [TextArea(2, 4)] private string bubbleText = "";

        private SpeechBubble     _bubble;
        private CircleCollider2D _trigger;

        private void Awake()
        {
            _bubble = GetComponent<SpeechBubble>();

            _trigger           = gameObject.AddComponent<CircleCollider2D>();
            _trigger.isTrigger = true;
            _trigger.radius    = WorldToLocalRadius(triggerRadius);
        }

        // CircleCollider2D.radius는 로컬 좌표 — lossyScale로 나눠서 월드 기준 반경으로 변환
        private float WorldToLocalRadius(float worldRadius)
        {
            float scale = Mathf.Abs(transform.lossyScale.x);
            return scale > 0.001f ? worldRadius / scale : worldRadius;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _bubble.Show(bubbleText);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _bubble.Hide();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var col = GetComponent<CircleCollider2D>();
            if (col == null || !col.isTrigger) return;
            float scale = Mathf.Abs(transform.lossyScale.x);
            col.radius = scale > 0.001f ? triggerRadius / scale : triggerRadius;
        }
#endif
    }
}
