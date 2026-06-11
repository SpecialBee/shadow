using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    // 플레이어 노출 상태 관리 + 그림자 판정.
    //   - Tick() : 플레이어 AABB 5점 샘플링 → EllipseShadow.All 순회, 3점 이상 포함 시 Shadow 판정
    //   - 우선순위 : Shadow > ExposedSight > Lit > Dark → SuspicionManager에 전달
    //   - LightSource 트리거(OnEnter/ExitDangerZone)와 NPC 위협 등록도 여기서 통합 관리
    public class PlayerExposureTracker : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.ShadowUpdate;

        // ── 노출 상태 ─────────────────────────────────────────────────────────

        public bool IsInShadow { get; private set; }

        private bool _inShadow;
        private int  _litCount;
        private int  _watchingCivilianCount;
        private readonly HashSet<NPCController> _threateningNpcs = new HashSet<NPCController>();
        private readonly HashSet<NPCController> _softThreatNpcs  = new HashSet<NPCController>();

        // ── 그림자 판정용 ─────────────────────────────────────────────────────

        private Collider2D     _col;
        private readonly Vector2[] _samplePts = new Vector2[5];

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
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

            int shadowedCount = 0;
            foreach (var pt in _samplePts)
            {
                foreach (var ellipse in EllipseShadow.All)
                {
                    if (ellipse.ContainsPoint(pt))
                    {
                        shadowedCount++;
                        break; // 같은 점이 두 타원에 동시에 포함돼도 1회만 카운트
                    }
                }
            }

            bool inShadow = shadowedCount >= 3;

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

        public void RegisterCivilianWatch()   { _watchingCivilianCount++; }
        public void UnregisterCivilianWatch() { _watchingCivilianCount = Mathf.Max(0, _watchingCivilianCount - 1); }

        // 동시에 보고 있는 Civilian 수에 따른 의심도 상승 배율
        public float GetCrowdMultiplier()
        {
            if (_watchingCivilianCount <= 1) return 1f;
            return Mathf.Min(2f, 1f + 0.3f * (_watchingCivilianCount - 1));
        }

        // ── 우선순위 판정: Shadow > ExposedSight > ExposedClose > Lit > Dark ──

        public void Evaluate()
        {
            ExposureState state;

            if (_inShadow)                          state = ExposureState.Shadow;
            else if (_threateningNpcs.Count > 0)    state = ExposureState.ExposedSight;
            else if (_softThreatNpcs.Count > 0)     state = ExposureState.ExposedClose;
            else if (_litCount > 0)                 state = ExposureState.Lit;
            else                                     state = ExposureState.Dark;

            SuspicionManager.Instance?.SetExposureState(state);
        }
    }
}
