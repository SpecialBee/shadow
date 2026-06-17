using UnityEngine;

namespace ShadowSeller.Core
{
    public class QuestTrigger : MonoBehaviour
    {
        public enum TriggerRole { ActivateQuest, AddProgress }
        public enum TriggerMode { Zone, OnExamine, OnTalk }

        public TriggerMode Mode => mode;

        [Header("퀘스트 연결")]
        [SerializeField] private QuestData   questData;
        [SerializeField] private TriggerRole role = TriggerRole.AddProgress;
        [SerializeField] private TriggerMode mode = TriggerMode.Zone;
        [SerializeField] private bool        oneShot = true;

        [Header("발동 조건 (선택)")]
        [Tooltip("이 퀘스트가 활성 상태일 때만 발동. 비워두면 조건 없음.")]
        [SerializeField] private QuestData requiredActiveQuest;

        [Header("완료 시 컷씬 (선택)")]
        [Tooltip("이 트리거로 인해 퀘스트가 완료될 때 컷씬 실행.")]
        [SerializeField] private bool            playCutsceneOnComplete;
        [SerializeField] private CutsceneStep[]  completionSteps;
        [SerializeField] private bool            overrideSpawnOnComplete;
        [SerializeField] private Transform       completionSpawnPoint;

        [Header("완료 시 플래그 (선택)")]
        [Tooltip("퀘스트 완료 시 CheckpointManager에 등록할 플래그.")]
        [SerializeField] private string completionMeetFlag;

        private bool _fired;

        // Zone 진입 시 자동 발동
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (mode != TriggerMode.Zone) return;
            if (!other.CompareTag("Player")) return;
            Fire();
        }

        // InteractableObject.DoExamine() 에서 직접 호출 (OnExamine 모드)
        public void Fire()
        {
            if (_fired && oneShot) return;
            if (questData == null) return;

            if (requiredActiveQuest != null &&
                (QuestManager.Instance == null || !QuestManager.Instance.IsActive(requiredActiveQuest.questId)))
                return;

            if (QuestManager.Instance == null) return;

            _fired = true;

            switch (role)
            {
                case TriggerRole.ActivateQuest:
                    QuestManager.Instance.ActivateQuest(questData);
                    break;

                case TriggerRole.AddProgress:
                    QuestManager.Instance.AddProgress(questData);
                    if (QuestManager.Instance.IsComplete(questData.questId))
                        HandleCompletion();
                    break;
            }
        }

        private void HandleCompletion()
        {
            if (!string.IsNullOrEmpty(completionMeetFlag))
                CheckpointManager.Instance?.RegisterCollected(completionMeetFlag);

            if (playCutsceneOnComplete && CutsceneDirector.Instance != null)
                CutsceneDirector.Instance.Play(completionSteps, overrideSpawnOnComplete, completionSpawnPoint);
        }

        private void OnDrawGizmos()
        {
            if (mode != TriggerMode.Zone) return;
            var col = GetComponent<CircleCollider2D>();
            if (col == null) return;
            Gizmos.color = role == TriggerRole.ActivateQuest
                ? new Color(0.2f, 0.8f, 1f, 0.3f)
                : new Color(0.2f, 1f, 0.4f, 0.3f);
            Gizmos.DrawWireSphere(transform.position + (Vector3)col.offset, col.radius);
        }
    }
}
