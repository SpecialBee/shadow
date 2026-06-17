using System.Collections.Generic;
using UnityEngine;

namespace ShadowSeller.Core
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public static event System.Action<QuestData>      OnQuestActivated;
        public static event System.Action<QuestData, int> OnQuestProgress;   // (data, currentCount)
        public static event System.Action<QuestData>      OnQuestCompleted;

        private struct QuestState
        {
            public bool isActive;
            public bool isComplete;
            public int  progress;
        }

        private readonly Dictionary<string, QuestState> _states = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ActivateQuest(QuestData data)
        {
            if (data == null) return;
            if (_states.TryGetValue(data.questId, out var s) && (s.isActive || s.isComplete)) return;

            _states[data.questId] = new QuestState { isActive = true };
            OnQuestActivated?.Invoke(data);
        }

        public void AddProgress(QuestData data)
        {
            if (data == null) return;
            if (!_states.TryGetValue(data.questId, out var s) || !s.isActive || s.isComplete) return;

            s.progress++;
            _states[data.questId] = s;
            OnQuestProgress?.Invoke(data, s.progress);

            if (s.progress >= data.totalCount)
            {
                s.isActive   = false;
                s.isComplete = true;
                _states[data.questId] = s;
                OnQuestCompleted?.Invoke(data);
            }
        }

        public bool IsActive(string questId) =>
            _states.TryGetValue(questId, out var s) && s.isActive;

        public bool IsComplete(string questId) =>
            _states.TryGetValue(questId, out var s) && s.isComplete;

        public int GetProgress(string questId) =>
            _states.TryGetValue(questId, out var s) ? s.progress : 0;
    }
}
