using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    // 플레이어 노출 상태 관리 + 그림자 판정 (ShadowSystem 흡수).
    //   - Tick() : 플레이어 AABB 5점 샘플링 → ShadowZone 3/5 이상 겹치면 Shadow 판정
    //   - 우선순위 : Shadow > ExposedSight > Lit > Dark → SuspicionManager에 전달
    //   - LightSource 트리거(OnEnter/ExitDangerZone)와 NPC 위협 등록도 여기서 통합 관리
    public class PlayerExposureTracker : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.ShadowUpdate;

        [SerializeField] private LayerMask shadowLayer;

        // ── 노출 상태 ─────────────────────────────────────────────────────────

        public bool IsInShadow { get; private set; }

        private bool _inShadow;
        private int  _litCount;
        private readonly HashSet<NPCController> _threateningNpcs = new HashSet<NPCController>();
        private readonly HashSet<NPCController> _softThreatNpcs  = new HashSet<NPCController>();

        // ── 그림자 판정용 ─────────────────────────────────────────────────────

        private Collider2D  _col;
        private readonly Vector2[]                   _samplePts  = new Vector2[5];
        private readonly Collider2D[]                _hitBuffer  = new Collider2D[8];
        private readonly Dictionary<ShadowZone, int> _zoneCounts = new Dictionary<ShadowZone, int>(8);
        private ContactFilter2D                      _filter;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();

            _filter = new ContactFilter2D();
            _filter.SetLayerMask(shadowLayer);
            _filter.useTriggers = true;

            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        // ── ITickable: ShadowUpdate 페이즈에서 그림자 판정 ───────────────────

        public void Tick()
        {
            Bounds  b  = _col != null
                ? _col.bounds
                : new Bounds(transform.position, Vector3.one * 0.5f);
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
                int n = Physics2D.OverlapPoint(pt, _filter, _hitBuffer);
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
                if (kv.Value >= 3) { inShadow = true; break; }

            _inShadow  = inShadow;
            IsInShadow = inShadow;
            Evaluate();
        }

        // ── Lit 판정 (LightSource 트리거 콜백) ───────────────────────────────

        public void OnEnterDangerZone() { _litCount++; Evaluate(); }
        public void OnExitDangerZone()  { _litCount = Mathf.Max(0, _litCount - 1); Evaluate(); }

        // ── NPC 위협 등록 ─────────────────────────────────────────────────────

        public void RegisterNpcThreat(NPCController npc)    { _threateningNpcs.Add(npc);    Evaluate(); }
        public void UnregisterNpcThreat(NPCController npc)  { _threateningNpcs.Remove(npc); Evaluate(); }

        public void RegisterSoftThreat(NPCController npc)   { _softThreatNpcs.Add(npc);    Evaluate(); }
        public void UnregisterSoftThreat(NPCController npc) { _softThreatNpcs.Remove(npc); Evaluate(); }

        // ── 우선순위 판정: Shadow > ExposedSight > ExposedClose > Dark ──────

        public void Evaluate()
        {
            ExposureState state;

            if (_inShadow)                          state = ExposureState.Shadow;
            else if (_threateningNpcs.Count > 0)    state = ExposureState.ExposedSight;
            else if (_softThreatNpcs.Count > 0)     state = ExposureState.ExposedClose;
            else                                     state = ExposureState.Dark;

            SuspicionManager.Instance?.SetExposureState(state);
        }
    }
}
