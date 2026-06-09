using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    public class ShadowSystem : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.ShadowUpdate;

        [SerializeField] private LayerMask shadowLayer;

        private PlayerController      _player;
        private PlayerExposureTracker _tracker;
        private Collider2D            _playerCollider;

        private readonly Vector2[]                  _samplePts = new Vector2[5];
        private readonly Collider2D[]               _hitBuffer = new Collider2D[8];
        private readonly Dictionary<ShadowZone,int> _zoneCounts = new Dictionary<ShadowZone,int>(8);

        private void Awake()
        {
            var playerGo    = GameObject.FindWithTag("Player");
            _player          = playerGo.GetComponent<PlayerController>();
            _tracker         = playerGo.GetComponent<PlayerExposureTracker>();
            _playerCollider  = playerGo.GetComponent<Collider2D>();
            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            Bounds  b  = _playerCollider != null
                ? _playerCollider.bounds
                : new Bounds(_player.transform.position, Vector3.one * 0.5f);
            Vector2 c  = b.center;
            float   hw = b.extents.x * 0.8f;
            float   hh = b.extents.y * 0.8f;

            _samplePts[0] = c;
            _samplePts[1] = c + new Vector2(-hw, -hh);
            _samplePts[2] = c + new Vector2( hw, -hh);
            _samplePts[3] = c + new Vector2(-hw,  hh);
            _samplePts[4] = c + new Vector2( hw,  hh);

            _zoneCounts.Clear();
            foreach (var pt in _samplePts)
            {
                int n = Physics2D.OverlapPointNonAlloc(pt, _hitBuffer, shadowLayer);
                for (int i = 0; i < n; i++)
                {
                    if (_hitBuffer[i].TryGetComponent<ShadowZone>(out var z))
                    {
                        _zoneCounts.TryGetValue(z, out int cnt);
                        _zoneCounts[z] = cnt + 1;
                    }
                }
            }

            bool inShadow = false;
            foreach (var kv in _zoneCounts)
            {
                if (kv.Value >= 3) { inShadow = true; break; }
            }

            _tracker.SetShadow(inShadow);
        }
    }
}
