using UnityEngine;

namespace ShadowSeller.Core
{
    // 전역 경계 레벨 관리 — Guard Chase 횟수와 고의심도 지속 시간을 기반으로 레벨 1~4 결정.
    //   레벨은 한 번 올라가면 내려가지 않음 (세션 내 지속 긴장감).
    //   레벨별 효과는 NPCController/SuspicionManager가 직접 읽어서 처리.
    //   포인트 기준: Guard Chase +2 / 의심도 70↑ 지속 10초 +1
    //   레벨 임계: 1=0~1pt / 2=2~3pt / 3=4~6pt / 4=7pt↑
    public class AlertManager : MonoBehaviour, ITickable
    {
        public static AlertManager Instance { get; private set; }
        public TickPhase Phase => TickPhase.AlertUpdate;

        public int Level { get; private set; } = 1;

        public static event System.Action<int> OnAlertLevelChanged;

        // Level 4에서 의심도 자연 감소율 80% 차단
        public float SuspicionDecayMult => Level >= 4 ? 0.2f : 1f;

        private int   _alertPoints;
        private float _highSuspicionTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameLoopController.Instance?.Unregister(this);
        }

        // Guard가 Alert/Chase 진입 시 NPCController에서 호출
        public void RegisterGuardChase()
        {
            _alertPoints += 2;
            EvaluateLevel();
        }

        // Civilian이 tier 3/4 진입 시 호출 (쿨다운은 NPCController에서 관리)
        public void RegisterCivilianAlert()
        {
            _alertPoints += 1;
            EvaluateLevel();
        }

        public void Tick()
        {
            float suspicion = SuspicionManager.Instance?.CurrentSuspicion ?? 0f;
            if (suspicion >= 70f)
            {
                _highSuspicionTimer += Time.deltaTime;
                if (_highSuspicionTimer >= 10f)
                {
                    _highSuspicionTimer = 0f;
                    _alertPoints++;
                    EvaluateLevel();
                }
            }
            else
            {
                _highSuspicionTimer = 0f;
            }
        }

        private void EvaluateLevel()
        {
            int newLevel;
            if      (_alertPoints >= 7) newLevel = 4;
            else if (_alertPoints >= 4) newLevel = 3;
            else if (_alertPoints >= 2) newLevel = 2;
            else                        newLevel = 1;

            if (newLevel <= Level) return;
            Level = newLevel;
            OnAlertLevelChanged?.Invoke(Level);
            Debug.Log($"[AlertManager] 경계 레벨 → {Level}");
        }
    }
}
