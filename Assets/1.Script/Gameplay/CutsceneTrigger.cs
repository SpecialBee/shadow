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
            _fired = true;

            var director = CutsceneDirector.Instance;
            if (director == null)
            {
                Debug.LogWarning("[CutsceneTrigger] CutsceneDirector가 씬에 없습니다.");
                return;
            }
            director.Play(steps, overridePlayerSpawn, spawnPoint);
        }

        private void OnDrawGizmos()
        {
            if (mode != TriggerMode.Interaction) return;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, approachRadius);
        }
    }
}
