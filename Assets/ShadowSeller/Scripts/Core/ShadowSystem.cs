using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    public class ShadowSystem : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.ShadowUpdate;

        [SerializeField] private LayerMask shadowLayer;

        // 위장 안정성 임계 거리 [unit] — index = (int)grade - 1
        // ShadowA: 0 (early-return 처리), B: 3u, C: 5u, D: ∞ (시야만으로 불안정)
        private static readonly float[] StabilityThresholds = { 0f, 3f, 5f, float.MaxValue };

        private PlayerController      _player;
        private PlayerExposureTracker _tracker;
        private Collider2D            _playerCollider;
        private NPCController[]       _npcs;

        // 매 틱 재사용 — GC 방지
        private readonly Vector2[]                  _samplePts  = new Vector2[5];
        private readonly Dictionary<ShadowZone,int> _zoneCounts = new Dictionary<ShadowZone,int>(8);

        private void Awake()
        {
            var playerGo   = GameObject.FindWithTag("Player");
            _player         = playerGo.GetComponent<PlayerController>();
            _tracker        = playerGo.GetComponent<PlayerExposureTracker>();
            _playerCollider = playerGo.GetComponent<Collider2D>();
            GameLoopController.Instance.Register(this);
        }

        private void Start()
        {
            _npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            // ── 5-포인트 샘플링: 플레이어 AABB의 80% 범위 내 중심+4모서리 ──────────
            Bounds b  = _playerCollider != null ? _playerCollider.bounds
                                                : new Bounds(_player.transform.position, Vector3.one * 0.5f);
            Vector2 c  = b.center;
            float   hw = b.extents.x * 0.8f;
            float   hh = b.extents.y * 0.8f;

            _samplePts[0] = c;
            _samplePts[1] = c + new Vector2(-hw, -hh);
            _samplePts[2] = c + new Vector2( hw, -hh);
            _samplePts[3] = c + new Vector2(-hw,  hh);
            _samplePts[4] = c + new Vector2( hw,  hh);

            // ── 존별 커버리지 집계 ──────────────────────────────────────────────────
            _zoneCounts.Clear();
            foreach (var pt in _samplePts)
            {
                var col = Physics2D.OverlapPoint(pt, shadowLayer);
                if (col == null || !col.TryGetComponent<ShadowZone>(out var z)) continue;
                _zoneCounts.TryGetValue(z, out int n);
                _zoneCounts[z] = n + 1;
            }

            // ── 커버리지 ≥ 3/5 인 존 중 가장 좋은 그레이드 선택 ──────────────────
            // (복수 존 겹침 시 ShadowA 우선 = 낮은 enum 값 우선)
            ShadowZone bestZone = null;
            foreach (var kv in _zoneCounts)
            {
                if (kv.Value < 3) continue;
                if (bestZone == null || kv.Key.Grade < bestZone.Grade)
                    bestZone = kv.Key;
            }

            if (bestZone == null)
            {
                _tracker.SetShadow(ExposureState.Dark);
                return;
            }

            ExposureState grade = bestZone.Grade;

            if (grade < ExposureState.ShadowA || grade > ExposureState.ShadowD)
            {
                _tracker.SetShadow(ExposureState.Dark);
                return;
            }

            if (IsUnstable(grade, _player.transform.position))
            {
                _tracker.SetShadow(ExposureState.ExposedSight);
                return;
            }

            _tracker.SetShadow(grade);
        }

        private bool IsUnstable(ExposureState grade, Vector2 playerPos)
        {
            if (grade <= ExposureState.ShadowA || grade > ExposureState.ShadowD) return false;

            float threshold = StabilityThresholds[(int)grade - 1];

            foreach (var npc in _npcs)
            {
                if (npc == null || !npc.IsSeeingPlayer) continue;
                if (Vector2.Distance(playerPos, npc.transform.position) <= threshold)
                    return true;
            }
            return false;
        }
    }
}
