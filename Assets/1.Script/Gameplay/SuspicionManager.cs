using UnityEngine;

namespace ShadowSeller.Core
{
    // 전역 의심도 관리 — ExposureState별 변화율을 매 틱 적용해 0~100 의심도를 갱신. 싱글턴.
    //   변화율은 Inspector에서 조절 가능 (의심도 변화율 헤더).
    //   Dark=0 / Shadow=-6 / Lit=+8 / ExposedClose=+5 / ExposedSight=+20 (기본값)
    //   노출 전환 순간 +15 스파이크 (재노출 시 한 번만).
    //   100 도달 또는 NPC 체포 시 OnGameOver 이벤트 발행 → GameOverUI가 수신.
    public class SuspicionManager : MonoBehaviour, ITickable
    {
        public static SuspicionManager Instance { get; private set; }

        public TickPhase Phase => TickPhase.SuspicionUpdate;

        public float         CurrentSuspicion { get; private set; }
        public ExposureState CurrentExposure  { get; private set; } = ExposureState.Dark;

        public static event System.Action<float>          OnSuspicionChanged;
        public static event System.Action<GameOverReason> OnGameOver;

        [Header("의심도 변화율 (/s)")]
        [SerializeField] private float rateDark         =  0f;
        [SerializeField] private float rateShadow       = -6f;
        [SerializeField] private float rateLit          =  8f;
        [SerializeField] private float rateExposedSight = 20f;
        [SerializeField] private float rateExposedClose =  5f;

        private bool _spikeArmed = true;
        private bool _gameOver;

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

        public void SetExposureState(ExposureState state)
        {
            bool wasExposed = IsExposed(CurrentExposure);
            bool nowExposed = IsExposed(state);

            if (nowExposed && !wasExposed && _spikeArmed)
            {
                CurrentSuspicion = Mathf.Min(100f, CurrentSuspicion + 15f);
                _spikeArmed = false;
            }
            else if (!nowExposed)
            {
                _spikeArmed = true;
            }

            CurrentExposure = state;
        }

        public void Tick()
        {
            if (_gameOver) return;

            float rate = CurrentExposure switch
            {
                ExposureState.Shadow       => rateShadow,
                ExposureState.Lit          => rateLit,
                ExposureState.ExposedSight => rateExposedSight,
                ExposureState.ExposedClose => rateExposedClose,
                _                          => rateDark,
            };
            // 레벨 4: 의심도 자연 감소율 80% 차단
            if (rate < 0f)
                rate *= AlertManager.Instance?.SuspicionDecayMult ?? 1f;
            CurrentSuspicion = Mathf.Clamp(CurrentSuspicion + rate * Time.deltaTime, 0f, 100f);

            OnSuspicionChanged?.Invoke(CurrentSuspicion);

            if (CurrentSuspicion >= 100f)
            {
                _gameOver = true;
                OnGameOver?.Invoke(GameOverReason.SuspicionFull);
            }
        }

        public void AddSuspicion(float amount)
        {
            if (_gameOver) return;
            CurrentSuspicion = Mathf.Clamp(CurrentSuspicion + amount, 0f, 100f);
            OnSuspicionChanged?.Invoke(CurrentSuspicion);
            if (CurrentSuspicion >= 100f)
            {
                _gameOver = true;
                OnGameOver?.Invoke(GameOverReason.SuspicionFull);
            }
        }

        public void TriggerArrest()
        {
            if (_gameOver) return;
            _gameOver = true;
            OnGameOver?.Invoke(GameOverReason.Arrested);
        }

        private static bool IsExposed(ExposureState s) =>
            s == ExposureState.ExposedSight || s == ExposureState.ExposedClose;
    }
}
