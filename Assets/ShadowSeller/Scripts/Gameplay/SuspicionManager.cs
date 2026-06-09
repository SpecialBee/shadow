using UnityEngine;

namespace ShadowSeller.Core
{
    public class SuspicionManager : MonoBehaviour, ITickable
    {
        public static SuspicionManager Instance { get; private set; }

        public TickPhase Phase => TickPhase.SuspicionUpdate;

        public float         CurrentSuspicion { get; private set; }
        public ExposureState CurrentExposure  { get; private set; } = ExposureState.Dark;

        public static event System.Action<float> OnSuspicionChanged;
        public static event System.Action        OnGameOver;

        // 변화율 테이블 [/s] — index = (int)ExposureState
        // Dark=-6, Shadow=0, Lit=+8, ExposedSight=+20, ExposedClose=+20
        private static readonly float[] Rates = { -6f, 0f, 8f, 20f, 20f };

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

            float rate = Rates[(int)CurrentExposure];
            CurrentSuspicion = Mathf.Clamp(CurrentSuspicion + rate * Time.deltaTime, 0f, 100f);

            OnSuspicionChanged?.Invoke(CurrentSuspicion);

            if (CurrentSuspicion >= 100f)
            {
                _gameOver = true;
                OnGameOver?.Invoke();
                UnityEngine.Debug.Log("[SuspicionManager] GAME OVER — 의심도 100 도달");
            }
        }

        public void TriggerArrest()
        {
            if (_gameOver) return;
            _gameOver = true;
            OnGameOver?.Invoke();
            Debug.Log("[SuspicionManager] 체포 — 게임 오버");
        }

        private static bool IsExposed(ExposureState s) =>
            s == ExposureState.ExposedSight || s == ExposureState.ExposedClose;
    }
}
