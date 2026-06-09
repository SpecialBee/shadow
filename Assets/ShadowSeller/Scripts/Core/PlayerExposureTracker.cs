using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    public class PlayerExposureTracker : MonoBehaviour
    {
        private int                         _litCount;
        private ExposureState               _shadowGrade = ExposureState.Dark;
        private readonly HashSet<NPCController> _threateningNpcs = new HashSet<NPCController>();

        // ── DangerZone 호출 ──────────────────────────────────────────────────

        public void OnEnterDangerZone() { _litCount++; Evaluate(); }
        public void OnExitDangerZone()  { _litCount = Mathf.Max(0, _litCount - 1); Evaluate(); }

        // ── ShadowSystem 호출 ────────────────────────────────────────────────

        public void SetShadow(ExposureState grade) { _shadowGrade = grade; Evaluate(); }

        // ── NPC 호출 ─────────────────────────────────────────────────────────

        public void RegisterNpcThreat(NPCController npc)   { _threateningNpcs.Add(npc);    Evaluate(); }
        public void UnregisterNpcThreat(NPCController npc) { _threateningNpcs.Remove(npc); Evaluate(); }

        // ── 우선순위 판정: EXPOSED > SHADOW(A~D) > LIT > DARK ────────────────

        public void Evaluate()
        {
            ExposureState state;

            bool exposed = _threateningNpcs.Count > 0
                        || _shadowGrade == ExposureState.ExposedSight
                        || _shadowGrade == ExposureState.ExposedClose;

            if (exposed)
                state = ExposureState.ExposedSight;
            else if (_shadowGrade >= ExposureState.ShadowA && _shadowGrade <= ExposureState.ShadowD)
                state = _shadowGrade;          // 그림자 안 → 그림자 품질 적용 (DangerZone 무시)
            else if (_litCount > 0)
                state = ExposureState.Lit;
            else
                state = ExposureState.Dark;

            SuspicionManager.Instance?.SetExposureState(state);
        }
    }
}
