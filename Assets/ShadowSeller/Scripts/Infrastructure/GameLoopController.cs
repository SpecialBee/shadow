using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameLoopController : MonoBehaviour
    {
        public static GameLoopController Instance { get; private set; }

        private readonly SortedDictionary<int, List<ITickable>> _phases =
            new SortedDictionary<int, List<ITickable>>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(ITickable tickable)
        {
            int key = (int)tickable.Phase;
            if (!_phases.ContainsKey(key))
                _phases[key] = new List<ITickable>();
            if (!_phases[key].Contains(tickable))
                _phases[key].Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            int key = (int)tickable.Phase;
            if (_phases.TryGetValue(key, out var list))
                list.Remove(tickable);
        }

        private void Update()
        {
            foreach (var list in _phases.Values)
                foreach (var t in list)
                    t.Tick();
        }
    }
}
