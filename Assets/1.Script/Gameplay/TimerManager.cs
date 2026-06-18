using UnityEngine;

namespace ShadowSeller.Core
{
    // 제한 시간 관리 싱글턴.
    //   시계 아이템을 획득하기 전까지 타이머는 존재하지만 UI에 표시되지 않음.
    //   시간 초과 시 SuspicionManager.TriggerTimeOver() → 패배 처리.
    public class TimerManager : MonoBehaviour, ITickable
    {
        public static TimerManager Instance { get; private set; }

        public TickPhase Phase => TickPhase.SuspicionUpdate;

        [Header("제한 시간")]
        [Tooltip("총 제한 시간 (초)")]
        [SerializeField] private float totalTime = 900f;

        [Header("시계 아이템")]
        [Tooltip("이 이름의 아이템을 획득하면 타이머가 UI에 공개됨")]
        [SerializeField] private string watchItemName = "시계";

        public float Remaining       { get; private set; }
        public float TotalTime       => totalTime;
        public float NormalizedLeft  => totalTime > 0f ? Remaining / totalTime : 0f;
        public bool  IsRevealed      { get; private set; }
        public bool  IsExpired       { get; private set; }

        public static event System.Action    OnTimerRevealed;
        public static event System.Action    OnTimerExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Remaining = totalTime;
            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameLoopController.Instance?.Unregister(this);
            InventoryManager.OnItemAdded -= OnItemAdded;
        }

        private void OnEnable()
        {
            InventoryManager.OnItemAdded += OnItemAdded;
        }

        private void OnDisable()
        {
            InventoryManager.OnItemAdded -= OnItemAdded;
        }

        private void OnItemAdded(int slot, InventoryManager.ItemData item)
        {
            if (IsRevealed) return;
            if (item.itemName == watchItemName)
            {
                IsRevealed = true;
                OnTimerRevealed?.Invoke();
            }
        }

        public void AddTime(float seconds)
        {
            if (IsExpired) return;
            Remaining = Mathf.Min(totalTime, Remaining + seconds);
        }

        public void Tick()
        {
            if (IsExpired) return;
            if (CutsceneDirector.Instance != null && CutsceneDirector.Instance.IsPlaying) return;

            Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);

            if (Remaining <= 0f)
            {
                IsExpired = true;
                OnTimerExpired?.Invoke();
                SuspicionManager.Instance?.TriggerTimeOver();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1))
                AddTime(60f);
#endif
        }
    }
}
