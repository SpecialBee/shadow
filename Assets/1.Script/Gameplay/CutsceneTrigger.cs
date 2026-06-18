using UnityEngine;

namespace ShadowSeller.Core
{
    public class CutsceneTrigger : MonoBehaviour
    {
        public enum TriggerMode { Zone, Interaction }

        [Header("트리거 설정")]
        [SerializeField] private TriggerMode mode           = TriggerMode.Zone;
        [SerializeField] private bool        oneShot        = true;
        [SerializeField] private float       approachRadius = 2f;

        [Header("컷씬 스텝")]
        [SerializeField] private CutsceneStep[] steps;

        [Header("컷씬 종료 후 플레이어 위치")]
        [SerializeField] private bool      overridePlayerSpawn = false;
        [SerializeField] private Transform spawnPoint;

        [Header("발동 조건")]
        [Tooltip("이 플래그가 등록되어 있어야 트리거 발동. 비워두면 조건 없음.")]
        [SerializeField] private string requiredMeetFlag;
        [Tooltip("인벤토리에 이 아이템이 있으면 트리거 발동 안 함. 비워두면 조건 없음.")]
        [SerializeField] private string blockedByItemName;

        [Header("컷씬 종료 후 처리")]
        [Tooltip("컷씬이 끝나면 비활성화할 오브젝트들 (NPC 등).")]
        [SerializeField] private GameObject[] deactivateOnEnd;
        [Tooltip("CheckpointManager에 등록할 플래그 이름. 비워두면 생략.")]
        [SerializeField] private string meetFlag;

        private bool _fired;

        private void Update()
        {
            if (mode != TriggerMode.Interaction) return;
            if (_fired && oneShot) return;

            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            float sqr = ((Vector2)transform.position - (Vector2)player.transform.position).sqrMagnitude;
            if (sqr > approachRadius * approachRadius) return;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;
#else
            if (!Input.GetKeyDown(KeyCode.E)) return;
#endif
            Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (mode != TriggerMode.Zone) return;
            if (_fired && oneShot) return;
            if (!other.CompareTag("Player")) return;
            Fire();
        }

        private void Fire()
        {
            if (_fired && oneShot) return;

            if (!string.IsNullOrEmpty(requiredMeetFlag) &&
                (CheckpointManager.Instance == null || !CheckpointManager.Instance.IsCollected(requiredMeetFlag)))
                return;

            if (!string.IsNullOrEmpty(blockedByItemName) &&
                InventoryManager.Instance != null && InventoryManager.Instance.HasItem(blockedByItemName))
                return;

            _fired = true;

            var director = CutsceneDirector.Instance;
            if (director == null)
            {
                Debug.LogWarning("[CutsceneTrigger] CutsceneDirector가 씬에 없습니다.");
                return;
            }

            director.Play(steps, overridePlayerSpawn, spawnPoint, OnCutsceneEnd);
        }

        private void OnCutsceneEnd()
        {
            if (deactivateOnEnd != null)
                foreach (var go in deactivateOnEnd)
                    if (go != null) go.SetActive(false);

            if (!string.IsNullOrEmpty(meetFlag))
                CheckpointManager.Instance?.RegisterCollected(meetFlag);
        }

        private void OnDrawGizmos()
        {
            if (mode != TriggerMode.Interaction) return;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, approachRadius);
        }
    }
}
