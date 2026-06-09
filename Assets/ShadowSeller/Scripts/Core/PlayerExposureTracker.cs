using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    public class PlayerExposureTracker : MonoBehaviour
    {
        public bool IsInShadow { get; private set; }

        private int  _litCount;
        private bool _inShadow;
        private readonly HashSet<NPCController> _threateningNpcs = new HashSet<NPCController>();

        public void OnEnterDangerZone() { _litCount++; Evaluate(); }
        public void OnExitDangerZone()  { _litCount = Mathf.Max(0, _litCount - 1); Evaluate(); }

        public void SetShadow(bool inShadow) { _inShadow = inShadow; IsInShadow = inShadow; Evaluate(); }

        public void RegisterNpcThreat(NPCController npc)   { _threateningNpcs.Add(npc);    Evaluate(); }
        public void UnregisterNpcThreat(NPCController npc) { _threateningNpcs.Remove(npc); Evaluate(); }

        // 우선순위: SHADOW > EXPOSED > LIT > DARK
        // 그림자 안에 있으면 NPC 위협보다 그림자가 우선 — 의심도 상승 차단
        public void Evaluate()
        {
            ExposureState state;

            if (_inShadow)
                state = ExposureState.Shadow;
            else if (_threateningNpcs.Count > 0)
                state = ExposureState.ExposedSight;
            else if (_litCount > 0)
                state = ExposureState.Lit;
            else
                state = ExposureState.Dark;

            SuspicionManager.Instance?.SetExposureState(state);
        }
    }
}
