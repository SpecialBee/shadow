using System.Collections;
using UnityEngine;

namespace ShadowSeller.Core
{
    // TriggerCollider 오브젝트에 붙이는 컴포넌트.
    // 플레이어가 진입하면 대사를 출력한 뒤 진입 방향 반대로 pushBackDistance만큼 밀어냄.
    [RequireComponent(typeof(Collider2D))]
    public class BlockingTrigger : MonoBehaviour
    {
        [Header("밀어낼 거리 (월드 단위)")]
        [SerializeField] private float pushBackDistance = 1.5f;

        [Header("대사 텍스트")]
        [SerializeField] private string message = "여기로는 갈 필요 없어";

        [Header("대사 출력 방식")]
        [Tooltip("true = DialogueSystem 팝업 / false = 즉시 물러남(대사 없이)")]
        [SerializeField] private bool useDialogueSystem = true;

        private bool _isBusy = false;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isBusy) return;
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            StartCoroutine(HandleBlock(player));
        }

        private IEnumerator HandleBlock(PlayerController player)
        {
            _isBusy = true;

            // PrologueDirector 등이 이미 IsLocked를 제어 중일 수 있으므로 상태 보존
            bool wasLocked = player.IsLocked;
            player.IsLocked = true;

            // 진입 방향의 반대 = 물러날 목표 지점
            Vector2 pushDir    = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            Vector2 pushTarget = (Vector2)player.transform.position + pushDir * pushBackDistance;

            if (useDialogueSystem && DialogueSystem.Instance != null)
            {
                var data  = ScriptableObject.CreateInstance<DialogueData>();
                data.lines = new DialogueLine[]
                {
                    new DialogueLine { speakerName = "", text = message }
                };

                bool dialogueDone = false;
                DialogueSystem.Instance.StartDialogue(data, () => dialogueDone = true);
                yield return new WaitUntil(() => dialogueDone);

                Destroy(data);
            }

            // WalkTo 대신 Rigidbody2D 직접 제어 — PrologueDirector의 WalkTo를 덮어쓰지 않음
            var rb = player.GetComponent<Rigidbody2D>();
            player.IsLocked = true;
            while (((Vector2)player.transform.position - pushTarget).sqrMagnitude > 0.06f * 0.06f)
            {
                Vector2 dir = (pushTarget - (Vector2)player.transform.position).normalized;
                rb.linearVelocity = dir * 3f;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            rb.position = pushTarget;

            // 진입 전 상태로 복원
            player.IsLocked = wasLocked;
            _isBusy = false;
        }
    }
}
