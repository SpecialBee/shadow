using UnityEngine;

namespace ShadowSeller.Core
{
    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }
        public bool IsComplete { get; private set; }

        public static event System.Action OnObjectiveComplete;
        public static event System.Action OnVictory;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Complete()
        {
            if (IsComplete) return;
            IsComplete = true;
            OnObjectiveComplete?.Invoke();
        }

        public void TriggerVictory()
        {
            if (!IsComplete) return;
            OnVictory?.Invoke();
        }
    }
}
